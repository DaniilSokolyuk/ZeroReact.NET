using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JavaScriptEngineSwitcher.ChakraCore;
using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.Core.Utilities;
using ZeroReact.Utils;

namespace ZeroReact.JsPool
{
    public interface IJavaScriptEngineFactory
    {
        /// <summary>
        /// Gets a JavaScript engine from the pool.
        /// </summary>
        /// <returns>The JavaScript engine</returns>
        ValueTask<JsEngineOwner> TakeEngineAsync(CancellationToken cancellationToken = default);
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
        private ZeroJsPool _pool;

        /// <summary>
        /// User script load exception
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
        private ZeroJsPool CreatePool()
        {
            var allFiles = _config.ScriptFilesWithoutTransform.Select(_fileSystem.MapPath);

            var poolConfig = new ZeroJsPoolConfig
            {
                EngineFactory = EngineFactory,
                StartEngines = _config.StartEngines,
                MaxEngines = _config.MaxEngines,
                MaxUsagesPerEngine = _config.MaxUsagesPerEngine
            };

            var pool = new ZeroJsPool(poolConfig);
            return pool;
        }

        private IJsEngine EngineFactory()
        {
            var engine = new ChakraCoreJsEngine(_config.EngineSettings);

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
                EnsureReactLoaded(engine);
            }

            return engine;
        }


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

        private void LoadUserScripts(IJsEngine engine)
        {
            foreach (var file in _config.ScriptFilesWithoutTransform)
            {
                try
                {
                    if (_config.AllowJavaScriptPrecompilation
                        && engine.TryExecuteFileWithPrecompilation(_cache, _fileSystem, file))
                    {
                        // Do nothing.
                    }
                    else
                    {
                        engine.Execute(_fileSystem.ReadAsString(file), file);
                    }
                }
                catch (JsException ex)
                {
                    _scriptLoadException = new ZeroReactException(string.Format(
                        "Error while loading \"{0}\": {1}",
                        file,
                        ex.Message
                    ), ex);
                }
                catch (IOException ex)
                {
                    _scriptLoadException = new ZeroReactException(ex.Message, ex);
                }
            }
        }

        private static void EnsureReactLoaded(IJsEngine engine)
        {
            var globalsString = engine.CallFunction<string>("ReactNET_initReact");
            string[] globals = globalsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (globals.Length != 0)
            {
                throw new ZeroReactException(
                    $"React has not been loaded correctly: missing ({string.Join(", ", globals)})." +
                    "Please expose your version of React as global variables named " +
                    "'React', 'ReactDOM', and 'ReactDOMServer', or enable the 'LoadReact'" +
                    "configuration option to use the built-in version of React."
                );
            }
        }

        public ValueTask<JsEngineOwner> TakeEngineAsync(CancellationToken cancellationToken = default)
        {
            EnsureValidState();
            return _pool.TakeAsync(cancellationToken);
        }

        private InterlockedStatedFlag disposed;
        public void Dispose()
        {
            if (disposed.Set())
            {
                if (_pool != null)
                {
                    _pool.Dispose();
                    _pool = null;
                }
            }
        }

        public void EnsureValidState()
        {
            if (disposed.IsSet())
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (_scriptLoadException != null)
            {
                throw _scriptLoadException;
            }
        }
    }
}