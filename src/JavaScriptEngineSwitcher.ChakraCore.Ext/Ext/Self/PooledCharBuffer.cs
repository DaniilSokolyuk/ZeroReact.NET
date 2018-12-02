using System;
using System.Buffers;

namespace JavaScriptEngineSwitcher.ChakraCore.JsRt
{    public readonly struct PooledCharBuffer : IDisposable
    {
        public PooledCharBuffer(char[] array, int length)
        {
            Array = array;
            Length = length;
        }

        public readonly char[] Array;

        public readonly int Length;

        public void Dispose()
        {
            if (Length > 0)
            {
                ArrayPool<char>.Shared.Return(Array);
            }
        }
    }
}