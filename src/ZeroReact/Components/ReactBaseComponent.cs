﻿using System;
using System.Buffers;
using System.IO;
using JavaScriptEngineSwitcher.ChakraCore.JsRt;
using Newtonsoft.Json;
using ZeroReact.JsPool;
using ZeroReact.Utils;

namespace ZeroReact.Components
{
    public abstract class ReactBaseComponent : IDisposable
    {
        protected readonly IJavaScriptEngineFactory _javaScriptEngineFactory;
        private readonly ReactConfiguration _configuration;
        private readonly IReactIdGenerator _reactIdGenerator;
        private readonly IComponentNameInvalidator _componentNameInvalidator;

        protected ReactBaseComponent(
            ReactConfiguration configuration,
            IReactIdGenerator reactIdGenerator,
            IJavaScriptEngineFactory javaScriptEngineFactory,
            IComponentNameInvalidator componentNameInvalidator)
        {
            _configuration = configuration;
            _reactIdGenerator = reactIdGenerator;
            _javaScriptEngineFactory = javaScriptEngineFactory;
            _componentNameInvalidator = componentNameInvalidator;

            ExceptionHandler = _configuration.ExceptionHandler;
        }

     
        private string _componentName;
        /// <summary>
        /// Gets or sets the name of the component
        /// </summary>
        public string ComponentName
        {
            get => _componentName;
            set
            {
                if (!_componentNameInvalidator.IsValid(value))
                {
                    ThrowHelper.ThrowComponentInvalidNameException(value);
                }

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

        private IMemoryOwner<char> SerializedProps { get; set; }
        protected void WriterSerialziedProps(TextWriter writer)
        {
            if (SerializedProps == null)
            {
                using (var pooledTextWriter = new ArrayPooledTextWriter())
                using (var jsonWriter = new JsonTextWriter(pooledTextWriter))
                {
                    jsonWriter.CloseOutput = false;
                    jsonWriter.AutoCompleteOnClose = false;
                    jsonWriter.ArrayPool = JsonArrayPool<char>.Instance;
                    _configuration.Serializer.Serialize(jsonWriter, Props);

                    SerializedProps = pooledTextWriter.GetMemoryOwner();
                }
            }

            writer.Write(SerializedProps.Memory.Span);
        }

        protected IMemoryOwner<char> OutputHtml { get; set; }
        public void WriteOutputHtmlTo(TextWriter writer)
        {
            if (ServerOnly)
            {
                writer.Write(OutputHtml.Memory.Span);
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
                writer.Write(OutputHtml.Memory.Span);
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
        public void RenderJavaScript(TextWriter writer)
        {
            writer.Write(ClientOnly ? "ReactDOM.render(React.createElement(" : "ReactDOM.hydrate(React.createElement(");
            writer.Write(ComponentName);
            writer.Write(',');
            WriterSerialziedProps(writer);
            writer.Write("),document.getElementById(\"");
            writer.Write(ContainerId);
            writer.Write("\"))");
        }

        public virtual void Dispose()
        {
            OutputHtml?.Dispose();
            SerializedProps?.Dispose();
        }
    }
}
