using System;
using System.Runtime.InteropServices;
using JavaScriptEngineSwitcher.ChakraCore.Constants;

namespace JavaScriptEngineSwitcher.ChakraCore.JsRt
{
    internal static partial class NativeMethods
    {

        [DllImport(DllName.Universal)]
        internal static extern JsErrorCode JsCreateStringUtf16(IntPtr buffer, uint length, out JsValue value);


        [DllImport(DllName.Universal)]
        internal static extern JsErrorCode JsCopyStringUtf16(JsValue value, int start, int length, IntPtr buffer, out UIntPtr written);
    }
}
