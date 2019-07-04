using System;
using System.Buffers;
using System.Threading.Tasks;
using JavaScriptEngineSwitcher.Core;
using Newtonsoft.Json;
using ZeroReact.JsPool;
using ZeroReact.Utils;

namespace ZeroReact.Components
{
    public sealed class ReactRouterComponent : ReactBaseComponent
    {
        public ReactRouterComponent(
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

        public string Path { get; set; }

        public RoutingContext RoutingContext { get; private set; }

        public Task RenderRouterWithContext()
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

                            using (var json = engine.Evaluate(StringifyJson))
                            {
                                RoutingContext = JsonConvert.DeserializeObject<RoutingContext>(new string(json.Memory.Span)); //TODO: manually on spans, model is easy
                            }
                        }
                        catch (JsRuntimeException ex)
                        {
                            ExceptionHandler(ex, ComponentName, ContainerId);
                        }
                    }
                });

            return work;
        }

        private static readonly ReadOnlyMemory<char> StringifyJson = "JSON.stringify(context);".AsMemory();

        private IMemoryOwner<char> GetEngineCodeExecute()
        {
            using (var textWriter = new ArrayPooledTextWriter())
            {
                textWriter.Write("var context={};");
                textWriter.Write(ServerOnly ? "ReactDOMServer.renderToStaticMarkup(React.createElement(" : "ReactDOMServer.renderToString(React.createElement(");
                textWriter.Write(ComponentName);
                textWriter.Write(",Object.assign(");
                WriterSerialziedProps(textWriter);
                textWriter.Write(",{location:\"");
                textWriter.Write(Path);
                textWriter.Write("\",context:context})))");

                return textWriter.GetMemoryOwner();
            }
        }
    }

    public class RoutingContext
    {
        /// <summary>
        /// HTTP Status Code.
        /// If present signifies that the given status code should be returned by server.
        /// </summary>
        public int? status { get; set; }

        /// <summary>
        /// URL to redirect to.
        /// If included this signals that React Router determined a redirect should happen.
        /// </summary>
        public string url { get; set; }
    }
}
