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
        private readonly IdGenerator _idGenerator = new IdGenerator();

        public IMemoryOwner<char> EvaluateUtf16String(ReadOnlyMemory<char> utf16Script) => _dispatcher.Invoke(() => Evaluate(utf16Script.Span));

        public Task<IMemoryOwner<char>> EvaluateUtf16StringAsync(ReadOnlyMemory<char> utf16Script) => _dispatcher.InvokeAsync(() => Evaluate(utf16Script.Span));

        private IMemoryOwner<char> Evaluate(ReadOnlySpan<char> utf16Script)
        {
            using (var uniqueDocumentName = _idGenerator.Generate())
            using (CreateJsScope())
            {
                try
                {
                    JsValue resultValue = JsContext.RunScriptUtf16Buffer(
                        utf16Script,
                        _jsSourceContext++,
                        uniqueDocumentName.Memory.Span);

                    return resultValue.JsCopyStringUtf16();
                }
                catch (JsException e)
                {
                    throw WrapJsException(e);
                }
            }
        }
    }
}
