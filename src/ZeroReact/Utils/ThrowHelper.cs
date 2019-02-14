using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ZeroReact
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
