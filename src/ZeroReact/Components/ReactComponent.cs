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

        public Task RenderHtml()
        {
            if (ClientOnly)
            {
                return Task.CompletedTask;
            }

            var work = _javaScriptEngineFactory.ScheduleWork(
                 engine =>
                 {
                     using (var executeEngineCode = GetEngineCodeExecute())
                     {
                         try
                         {
                             OutputHtml = engine.Evaluate(executeEngineCode.Memory);
                         }
                         catch (JsRuntimeException ex)
                         {
                             ExceptionHandler(ex, ComponentName, ContainerId);
                         }
                     }
                 });

            return work;
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