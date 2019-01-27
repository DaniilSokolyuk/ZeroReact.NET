using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JavaScriptEngineSwitcher.ChakraCore;
using JavaScriptEngineSwitcher.ChakraCore.JsRt;
using JavaScriptEngineSwitcher.Core;
using Newtonsoft.Json;
using ZeroReact.JsPool;
using ZeroReact.Utils;

namespace ZeroReact
{
    /// <summary>
    /// Represents a React JavaScript component.
    /// </summary>
    public class ReactComponent : IDisposable
    {
        private readonly ReactConfiguration _configuration;
        private readonly IReactIdGenerator _reactIdGenerator;
        private readonly IJavaScriptEngineFactory _javaScriptEngineFactory;

        public ReactComponent(ReactConfiguration configuration, IReactIdGenerator reactIdGenerator,
            IJavaScriptEngineFactory javaScriptEngineFactory)
        {
            _configuration = configuration;
            _reactIdGenerator = reactIdGenerator;
            _javaScriptEngineFactory = javaScriptEngineFactory;

            ExceptionHandler = _configuration.ExceptionHandler;
        }

        private static readonly ConcurrentDictionary<string, bool> _componentNameValidCache =
            new ConcurrentDictionary<string, bool>(StringComparer.Ordinal);

        /// <summary>
        /// Regular expression used to validate JavaScript identifiers. Used to ensure component
        /// names are valid.
        /// Based off https://gist.github.com/Daniel15/3074365
        /// </summary>
        private static readonly Regex _identifierRegex =
            new Regex(@"^[a-zA-Z_$][0-9a-zA-Z_$]*(?:\[(?:"".+""|\'.+\'|\d+)\])*?$", RegexOptions.Compiled);

        private string _componentName;

        /// <summary>
        /// Gets or sets the name of the component
        /// </summary>
        public string ComponentName
        {
            get => _componentName;
            set
            {
                EnsureComponentNameValid(value);
                _componentName = value;
            }
        }

        private string _containerId;

        /// <summary>
        /// Gets or sets the unique ID for the DIV container of this component
        /// </summary>
        public string ContainerId
        {
            get => _containerId ?? (_containerId = _reactIdGenerator.Generate());
            set => _containerId = value;
        }

        /// <summary>
        /// Gets or sets the HTML tag the component is wrapped in
        /// </summary>
        public string ContainerTag { get; set; } = "div";

        /// <summary>
        /// Gets or sets the HTML class for the container of this component
        /// </summary>
        public string ContainerClass { get; set; }

        /// <summary>
        /// Get or sets if this components only should be rendered server side
        /// </summary>
        public bool ServerOnly { get; set; }


        private bool _clientOnly;

        /// <summary>
        /// Get or sets if this components only should be rendered client side
        /// </summary>
        public bool ClientOnly
        {
            get => !_configuration.UseServerSideRendering || _clientOnly;
            set => _clientOnly = value;
        }

        /// <summary>
        /// Sets the props for this component
        /// </summary>
        public object Props { get; set; }

        public Action<Exception, string, string> ExceptionHandler { get; set; }

        public virtual async Task RenderHtml()
        {
            if (ClientOnly) 
                return;

            using (var pooledTextWriter = new ArrayPooledTextWriter())
            {
                pooledTextWriter.Write(ServerOnly
                    ? "ReactDOMServer.renderToStaticMarkup("
                    : "ReactDOMServer.renderToString(");

                var initOffset = pooledTextWriter.Length;
                WriteComponentInitialiser(pooledTextWriter);
                var initCount = pooledTextWriter.Length - initOffset;

                pooledTextWriter.Write(')');

                ExecuteEngineCode = pooledTextWriter.GetMemoryOwner();
                ComponentInitialiser = ExecuteEngineCode.Memory.Slice(initOffset, initCount);
            }

            try
            {
                using (var engine = _javaScriptEngineFactory.GetEngine()) //TODO: make it async
                {
                    var asChakra = (ChakraCoreJsEngine) engine.InnerEngine; //and remove this

                    Html = await asChakra.EvaluateUtf16StringAsync(ExecuteEngineCode.Memory);
                }
            }
            catch (JsRuntimeException ex)
            {
                ExceptionHandler(ex, ComponentName, ContainerId);
            }
        }

        public virtual void WriteRenderedHtmlTo(TextWriter writer)
        {
            if (ServerOnly)
            {
                writer.Write(Html.Memory.Span);
                return;
            }

            writer.Write('<');
            writer.Write(ContainerTag);
            writer.Write(" id=\"");
            writer.Write(ContainerId);
            writer.Write('"');
            if (!string.IsNullOrEmpty(ContainerClass))
            {
                writer.Write(" class=\"");
                writer.Write(ContainerClass);
                writer.Write('"');
            }

            writer.Write('>');

            if (!ClientOnly)
            {
                writer.Write(Html.Memory.Span);
            }

            writer.Write("</");
            writer.Write(ContainerTag);
            writer.Write('>');
        }

        /// <summary>
        /// Renders the JavaScript required to initialise this component client-side. This will
        /// initialise the React component, which includes attach event handlers to the
        /// server-rendered HTML.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.IO.TextWriter" /> to which the content is written</param>
        /// <returns>JavaScript</returns>
        public virtual void RenderJavaScript(TextWriter writer)
        {
            writer.Write(ClientOnly ? "ReactDOM.render(" : "ReactDOM.hydrate(");

            if (ComponentInitialiser.IsEmpty)
            {
                //render html is not called serialize directly :(
                WriteComponentInitialiser(writer);
            }
            else
            {
                writer.Write(ComponentInitialiser.Span);
            }

            writer.Write(", document.getElementById(\"");
            writer.Write(ContainerId);
            writer.Write("\"))");
        }

        protected IMemoryOwner<char> ExecuteEngineCode { get; set; }

        protected IMemoryOwner<char> Html { get; set; }

        /// <summary>
        /// Slice of ExecuteEngineCode
        /// </summary>
        protected ReadOnlyMemory<char> ComponentInitialiser { get; set; }

        protected virtual void WriteComponentInitialiser(TextWriter textWriter)
        {
            textWriter.Write("React.createElement(");
            textWriter.Write(ComponentName);
            textWriter.Write(", ");

            using (var jsonWriter = new JsonTextWriter(textWriter))
            {
                jsonWriter.CloseOutput = false;
                jsonWriter.AutoCompleteOnClose = false;
                jsonWriter.ArrayPool = JsonArrayPool<char>.Instance;
                _configuration.Serializer.Serialize(jsonWriter, Props);
            }

            textWriter.Write(')');
        }

        /// <summary>
        /// Validates that the specified component name is valid
        /// </summary>
        /// <param name="componentName"></param>
        internal static void EnsureComponentNameValid(string componentName)
        {
            if (!_componentNameValidCache.GetOrAdd(componentName,
                compName => compName.Split('.').All(segment => _identifierRegex.IsMatch(segment))))
            {
                throw new Exception($"Invalid component name '{componentName}'");
            }
        }

        public void Dispose()
        {
            ExecuteEngineCode.Dispose();
            Html.Dispose();
        }
    }
}