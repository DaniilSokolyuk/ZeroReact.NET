using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using JavaScriptEngineSwitcher.ChakraCore.Ext.Self;
using JavaScriptEngineSwitcher.ChakraCore.JsRt;
using JavaScriptEngineSwitcher.Core;
using JsException = JavaScriptEngineSwitcher.ChakraCore.JsRt.JsException;

namespace JavaScriptEngineSwitcher.ChakraCore
{
    public partial class ChakraCoreJsEngine
    {
        private readonly IdGenerator idGenerator = new IdGenerator();

        public PooledCharBuffer EvaluateUtf16String(PooledCharBuffer utf16script) => _dispatcher.Invoke(() => Evaluate(utf16script));

        public Task<PooledCharBuffer> EvaluateUtf16StringAsync(PooledCharBuffer utf16script) => _dispatcher.InvokeAsync(() => Evaluate(utf16script));

        private PooledCharBuffer Evaluate(PooledCharBuffer utf16script)
        {
            using (var uniqueDocumentName = idGenerator.Generate())
            using (CreateJsScope())
            {
                try
                {
                    JsValue resultValue = JsContext.RunScriptUtf16Buffer(
                        ref utf16script,
                        _jsSourceContext++,
                        uniqueDocumentName);

                    var processedValue = resultValue.ConvertToString();

                    return processedValue.ToStringUtf16StringAsPooledBuffer();
                }
                catch (JsException e)
                {
                    throw WrapJsException(e);
                }
            }
        }
    }
}
