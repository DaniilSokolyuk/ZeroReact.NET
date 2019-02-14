using System.IO;
using System.Threading.Tasks;
using ZeroReact.JsPool;

namespace ZeroReact.Components
{
    public sealed class ReactRouter : ReactBaseComponent
    {
        public string Path { get; set; }

        public ReactRouter(ReactConfiguration configuration, IReactIdGenerator reactIdGenerator, IJavaScriptEngineFactory javaScriptEngineFactory, IComponentNameInvalidator componentNameInvalidator) : base(configuration, reactIdGenerator, javaScriptEngineFactory, componentNameInvalidator)
        {
        }

        //public async Task<RoutingContext> RenderRouterWithContext()
        //{
           
        //}

        private void WriteComponentInitialiser(TextWriter textWriter)
        {
            textWriter.Write("React.createElement(");
            textWriter.Write(ComponentName);
            textWriter.Write(", Object.assign(");
            WriterSerialziedProps(textWriter);
            textWriter.Write(", { location: '");
            textWriter.Write(Path);
            textWriter.Write("', context: context }))");
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
            writer.Write("ReactDOM.hydrate("); //?render?

            writer.Write("React.createElement(");
            writer.Write(ComponentName);
            writer.Write(", ");
            WriterSerialziedProps(writer);
            writer.Write(')');

            writer.Write(", document.getElementById(\"");
            writer.Write(ContainerId);
            writer.Write("\"))");
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
