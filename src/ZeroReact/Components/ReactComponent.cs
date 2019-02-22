using System.Buffers;
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
            {
                return;
            }

            var task = _javaScriptEngineFactory.TakeEngineAsync();

            using (var executeEngineCode = GetEngineCodeExecute())
            using (var engineOwner = task.IsCompletedSuccessfully ? task.Result : await task)
            {
                try
                {
                    OutputHtml = (IMemoryOwner<char>)await ((ChakraCoreJsEngine)engineOwner.Engine).EvaluateUtf16StringAsync(executeEngineCode.Memory);
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

        private IMemoryOwner<char> GetEngineCodeExecute()
        {
            using (var writer = new ArrayPooledTextWriter())
            {
                writer.Write(ServerOnly ? "ReactDOMServer.renderToStaticMarkup(React.createElement(" : "ReactDOMServer.renderToString(React.createElement(");
                writer.Write(ComponentName);
                writer.Write(',');
                WriterSerialziedProps(writer);
                writer.Write("))");

                return writer.GetMemoryOwner();
            }
        }
    }
}