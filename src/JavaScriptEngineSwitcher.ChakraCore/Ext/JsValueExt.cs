using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using JavaScriptEngineSwitcher.ChakraCore.Ext.Self;

namespace JavaScriptEngineSwitcher.ChakraCore.JsRt
{
    internal partial struct JsValue
    {
        internal IMemoryOwner<char> JsCopyStringUtf16Pooled()
        {
            const int start = 0;
            int length = int.MaxValue;

            var errorCode = NativeMethods.JsCopyStringUtf16(this, start, length, null, out var written);
            JsErrorHelpers.ThrowIfError(errorCode);

            length = (int)written;
            var buffer = ArrayPool<char>.Shared.Rent(length);

            
            unsafe
            {
                fixed (char* bufferPtr = buffer)
                {
                    JsErrorHelpers.ThrowIfError(NativeMethods.JsCopyStringUtf16(this, start, length, (IntPtr)bufferPtr, out written));
                }
            }

            return new PooledCharBuffer(buffer, length);
        }
    }
}
