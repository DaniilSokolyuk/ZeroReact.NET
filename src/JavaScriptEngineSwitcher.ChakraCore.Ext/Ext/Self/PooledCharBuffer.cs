using System;
using System.Buffers;

namespace JavaScriptEngineSwitcher.ChakraCore.JsRt
{
    public sealed class PooledCharBuffer : IMemoryOwner<char>
    {
        public PooledCharBuffer(char[] array, int length)
        {
            _array = array;
            _length = length;
        }

        private char[] _array;

        private int _length;

        public void Dispose()
        {
            if (_length > 0)
            {
                ArrayPool<char>.Shared.Return(_array);
            }
        }

        public Memory<char> Memory
        {
            get
            {
                var array = _array;

                return new Memory<char>(array, 0, _length);
            }
        }
    }
}