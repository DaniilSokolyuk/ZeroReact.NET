using System;
using System.IO;
using System.Linq;
using System.Reflection;
using JavaScriptEngineSwitcher.ChakraCore;
using JavaScriptEngineSwitcher.Core;
using JSPool;
using ZeroReact.Utils;

namespace ZeroReact.JsPool
{
    public interface IJavaScriptEngineFactory
    {
        /// <summary>
        /// Gets a JavaScript engine from the pool.
        /// </summary>
        /// <returns>The JavaScript engine</returns>
        PooledJsEngine GetEngine();
    }

    /// <summary>
	/// Handles creation of JavaScript engines. All methods are thread-safe.
	/// </summary>
	public sealed class JavaScriptEngineFactory : IDisposable, IJavaScriptEngineFactory
    {
		/// <summary>
		/// React configuration for the current site
		/// </summary>
		private readonly ReactConfiguration _config;
		/// <summary>
		/// Cache used for storing the pre-compiled scripts
		/// </summary>
		private readonly ICache _cache;
		/// <summary>`
		/// File system wrapper
		/// </summary>
		private readonly IFileSystem _fileSystem;

		/// <summary>
		/// Pool of JavaScript engines to use
		/// </summary>
		private IJsPool _pool;
		/// <summary>
		/// Whether this class has been disposed.
		/// </summary>
		private bool _disposed;
		/// <summary>
		/// The exception that was thrown during the most recent recycle of the pool.
		/// </summary>
		private Exception _scriptLoadException;

		/// <summary>
		/// Initializes a new instance of the <see cref="JavaScriptEngineFactory"/> class.
		/// </summary>
		public JavaScriptEngineFactory(
			ReactConfiguration config,
			ICache cache,
			IFileSystem fileSystem
		)
		{
			_config = config;
			_cache = cache;
			_fileSystem = fileSystem;
            _pool = CreatePool();
		}

		/// <summary>
		/// Creates a new JavaScript engine pool.
		/// </summary>
		private IJsPool CreatePool()
		{
			var allFiles = _config.ScriptFilesWithoutTransform.Select(_fileSystem.MapPath);

		    var poolConfig = new JsPoolConfig
		    {
		        EngineFactory = () => new ChakraCoreJsEngine(_config.EngineSettings),
		        Initializer = InitialiseEngine,
		        WatchPath = _fileSystem.MapPath("~/"),
		        WatchFiles = allFiles,
		        StartEngines = _config.StartEngines,
		        MaxEngines = _config.MaxEngines,
                MaxUsagesPerEngine = _config.MaxUsagesPerEngine
		    };

		    var pool = new CustomJsPool(poolConfig);
			// Reset the recycle exception on recycle. If there *are* errors loading the scripts
			// during recycle, the errors will be caught in the initializer.
			pool.Recycled += (sender, args) => _scriptLoadException = null;
			return pool;
		}

		/// <summary>
		/// Loads standard React and Babel scripts into the engine.
		/// </summary>
		private void InitialiseEngine(IJsEngine engine)
		{
			var thisAssembly = typeof(ReactConfiguration).GetTypeInfo().Assembly;
			LoadResource(engine, "ZeroReact.Resources.shims.js", thisAssembly);

			if (_config.LoadReact)
			{
				LoadResource(
					engine,
					_config.UseDebugReact
						? "ZeroReact.Resources.react.generated.js"
                        : "ZeroReact.Resources.react.generated.min.js",
					thisAssembly
				);
			}

		    LoadUserScripts(engine);
            if (!_config.LoadReact && _scriptLoadException == null)
			{
				// We expect to user to have loaded their own version of React in the scripts that
				// were loaded above, let's ensure that's the case.
				EnsureReactLoaded(engine);
			}
		}

		/// <summary>
		/// Loads code from embedded JavaScript resource into the engine.
		/// </summary>
		/// <param name="engine">Engine to load a code from embedded JavaScript resource</param>
		/// <param name="resourceName">The case-sensitive resource name</param>
		/// <param name="assembly">The assembly, which contains the embedded resource</param>
		private void LoadResource(IJsEngine engine, string resourceName, Assembly assembly)
		{
			if (_config.AllowJavaScriptPrecompilation && engine.TryExecuteResourceWithPrecompilation(_cache, resourceName, assembly))
			{
				// Do nothing.
			}
			else
			{
				engine.ExecuteResource(resourceName, assembly);
			}
		}

	    /// <summary>
	    /// Loads any user-provided scripts. Only scripts that don't need JSX transformation can
	    /// run immediately here. JSX files are loaded in ReactEnvironment.
	    /// </summary>
	    /// <param name="engine">Engine to load scripts into</param>
	    private void LoadUserScripts(IJsEngine engine)
	    {
	        foreach (var file in _config.ScriptFilesWithoutTransform)
	        {
	            try
	            {
	                if (_config.AllowJavaScriptPrecompilation && engine.TryExecuteFileWithPrecompilation(_cache, _fileSystem, file))
	                {
	                    // Do nothing.
	                }
	                else
	                {
	                    engine.Execute(_fileSystem.ReadAsString(file), file);
	                }
	            }
	            catch (JsScriptException ex)
	            {
	                // We can't simply rethrow the exception here, as it's possible this is running
	                // on a background thread (ie. as a response to a file changing). If we did
	                // throw the exception here, it would terminate the entire process. Instead,
	                // save the exception, and then just rethrow it later when getting the engine.
	                _scriptLoadException = new ReactException(
	                    $"Error while loading \"{file}\": {ex.Message}\r\nLine: {ex.LineNumber}\r\nColumn: {ex.ColumnNumber}");
	            }
	            catch (IOException ex)
	            {
	                _scriptLoadException = new ReactException(ex.Message);
	            }
	        }
	    }

        /// <summary>
        /// Ensures that React has been correctly loaded into the specified engine.
        /// </summary>
        /// <param name="engine">Engine to check</param>
        private static void EnsureReactLoaded(IJsEngine engine)
		{
			var globalsString = engine.CallFunction<string>("ReactNET_initReact");
			string[] globals = globalsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			if (globals.Length != 0)
			{
				throw new ReactException(
					$"React has not been loaded correctly: missing ({string.Join(", ", globals)})." +
					"Please expose your version of React as global variables named " +
					"'React', 'ReactDOM', and 'ReactDOMServer', or enable the 'LoadReact'" +
					"configuration option to use the built-in version of React."
				);
			}
		}

		/// <summary>
		/// Gets a JavaScript engine from the pool.
		/// </summary>
		/// <returns>The JavaScript engine</returns>
		public PooledJsEngine GetEngine()
		{
			EnsureValidState();
			return _pool.GetEngine();
		}

		/// <summary>
		/// Clean up all engines
		/// </summary>
		public void Dispose()
		{
			_disposed = true;
			if (_pool != null)
			{
				_pool.Dispose();
				_pool = null;
			}
		}

		/// <summary>
		/// Ensures that this object has not been disposed, and that no error was thrown while
		/// loading the scripts.
		/// </summary>
		public void EnsureValidState()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(GetType().Name);
			}
			if (_scriptLoadException != null)
			{
				// This means an exception occurred while loading the script (eg. syntax error in the file)
				throw _scriptLoadException;
			}
		}
	}
}