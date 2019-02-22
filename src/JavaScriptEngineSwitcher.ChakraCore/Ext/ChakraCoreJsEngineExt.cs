using System;
using System.Buffers;
using System.Threading.Tasks;
using JavaScriptEngineSwitcher.ChakraCore.Ext.Self;
using JavaScriptEngineSwitcher.ChakraCore.JsRt;
using JsException = JavaScriptEngineSwitcher.ChakraCore.JsRt.JsException;

namespace JavaScriptEngineSwitcher.ChakraCore
{
    public partial class ChakraCoreJsEngine
    {
        private readonly IdGenerator _idGenerator = new IdGenerator();

        /// <summary>
        /// Schedule item to work
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public Task Schedule(Action<ChakraCoreJsEngine> action) =>
            _dispatcher.InvokeAsync(
                () =>
                {
                    action(this);
                    return null;
                });

        public IMemoryOwner<char> Evaluate(ReadOnlyMemory<char> utf16Script)
        {
            using (var uniqueDocumentName = _idGenerator.Generate())
            using (CreateJsScope())
            {
                try
                {
                    JsValue resultValue = JsContext.RunScriptUtf16Buffer(
                        utf16Script.Span,
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
