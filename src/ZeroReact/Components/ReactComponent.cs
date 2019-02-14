using System.IO;
using System.Threading.Tasks;
using JavaScriptEngineSwitcher.ChakraCore;
using JavaScriptEngineSwitcher.Core;
using ZeroReact.JsPool;
using ZeroReact.Utils;

namespace ZeroReact.Components
{
    /// <summary>
    /// Represents a React JavaScript component.
    /// </summary>
    public sealed class ReactComponent : ReactBaseComponent
    {
        public ReactComponent(
            ReactConfiguration configuration,
            IReactIdGenerator reactIdGenerator,
            IJavaScriptEngineFactory javaScriptEngineFactory,
            IComponentNameInvalidator componentNameInvalidator) : base(
            configuration,
            reactIdGenerator,
            javaScriptEngineFactory,
            componentNameInvalidator)
        {
        }

        public async Task RenderHtml()
        {
            if (ClientOnly)
                return;

            using (var pooledTextWriter = new ArrayPooledTextWriter())
            {
                pooledTextWriter.Write(ServerOnly
                    ? "ReactDOMServer.renderToStaticMarkup("
                    : "ReactDOMServer.renderToString(");

                WriteComponentInitialiser(pooledTextWriter);

                pooledTextWriter.Write(')');

                var executeEngineCode = pooledTextWriter.GetMemoryOwner(true);
                try
                {
                    using (var engineOwner = await _javaScriptEngineFactory.TakeEngineAsync())
                    {
                        OutputHtml = await ((ChakraCoreJsEngine)engineOwner.Engine).EvaluateUtf16StringAsync(executeEngineCode.Memory);
                    }
                }
                catch (JsRuntimeException ex)
                {
                    ExceptionHandler(ex, ComponentName, ContainerId);
                }
                finally
                {
                    executeEngineCode.Dispose();
                }
            }
        }

        /// <summary>
        /// Renders the JavaScript required to initialise this component client-side. This will
        /// initialise the React component, which includes attach event handlers to the
        /// server-rendered HTML.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.IO.TextWriter" /> to which the content is written</param>
        /// <returns>JavaScript</returns>
        public override void RenderJavaScript(TextWriter writer)
        {
            writer.Write(ClientOnly ? "ReactDOM.render(" : "ReactDOM.hydrate(");
            WriteComponentInitialiser(writer);
            writer.Write(", document.getElementById(\"");
            writer.Write(ContainerId);
            writer.Write("\"))");
        }

        private void WriteComponentInitialiser(TextWriter textWriter)
        {
            textWriter.Write("React.createElement(");
            textWriter.Write(ComponentName);
            textWriter.Write(',');
            WriterSerialziedProps(textWriter);
            textWriter.Write(')');
        }
    }
}