using System;
using System.Diagnostics;

namespace ZeroReact.Utils
{
    [DebuggerStepThrough]
    internal static class ThrowHelper
    {
        public static void ThrowComponentInvalidNameException(string value)
        {
            throw new ArgumentException($"Invalid component name '{value}'");
        }
    }
}
