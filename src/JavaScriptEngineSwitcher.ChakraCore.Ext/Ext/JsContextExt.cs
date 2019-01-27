using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using JavaScriptEngineSwitcher.ChakraCore.Ext.Self;

namespace JavaScriptEngineSwitcher.ChakraCore.JsRt
{
    internal partial struct JsContext
    {
        public static unsafe JsValue RunScriptUtf16Buffer(
            ref PooledCharBuffer scriptBuffer,
            JsSourceContext sourceContext,
            PooledCharBuffer sourceUrl)
        {
            //not work, because fixed.. https://github.com/dotnet/corefx/issues/31651 PInvoke marshaling will pin the pointer for ref byte hash arguments that will pin the whole block as side-effect
            
            fixed (char* scriptBufferPtr = &MemoryMarshal.GetReference(scriptBuffer.Memory.Span))
            fixed (char* sourceUrlPtr = &MemoryMarshal.GetReference(sourceUrl.Memory.Span))
            {
                JsErrorHelpers.ThrowIfError(NativeMethods.JsCreateStringUtf16((IntPtr)scriptBufferPtr, (uint)scriptBuffer.Memory.Length, out var scriptValue));
                scriptValue.AddRef();

                JsErrorHelpers.ThrowIfError(NativeMethods.JsCreateStringUtf16((IntPtr)sourceUrlPtr, (uint)sourceUrl.Memory.Length, out var sourceUrlValue));
                sourceUrlValue.AddRef();

                JsValue result;

                try
                {
                    JsErrorHelpers.ThrowIfError(NativeMethods.JsRun(scriptValue, sourceContext, sourceUrlValue, JsParseScriptAttributes.None, out result));
                }
                finally
                {
                    scriptValue.Release();
                    sourceUrlValue.Release();
                }

                return result;
            }
        }
    }
}
