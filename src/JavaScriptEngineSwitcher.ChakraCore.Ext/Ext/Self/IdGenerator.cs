using System;
using System.Buffers;
using System.Threading;
using JavaScriptEngineSwitcher.ChakraCore.JsRt;

namespace JavaScriptEngineSwitcher.ChakraCore.Ext.Self
{
    public class IdGenerator
    {
        private static readonly string _encode32Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV";

        private long _random;

        private static readonly char[] reactPrefix = "script_".ToCharArray();

        private const int reactIdLength = 20;

        public PooledCharBuffer Generate()
        {
            
            var chars = ArrayPool<char>.Shared.Rent(reactIdLength);
            Array.Copy(reactPrefix, 0, chars, 0, reactPrefix.Length);

            var id = Interlocked.Increment(ref _random);

            chars[7] = _encode32Chars[(int)(id >> 60) & 31];
            chars[8] = _encode32Chars[(int)(id >> 55) & 31];
            chars[9] = _encode32Chars[(int)(id >> 50) & 31];
            chars[10] = _encode32Chars[(int)(id >> 45) & 31];
            chars[11] = _encode32Chars[(int)(id >> 40) & 31];
            chars[12] = _encode32Chars[(int)(id >> 35) & 31];
            chars[13] = _encode32Chars[(int)(id >> 30) & 31];
            chars[14] = _encode32Chars[(int)(id >> 25) & 31];
            chars[15] = _encode32Chars[(int)(id >> 20) & 31];
            chars[16] = _encode32Chars[(int)(id >> 15) & 31];
            chars[17] = _encode32Chars[(int)(id >> 10) & 31];
            chars[18] = _encode32Chars[(int)(id >> 5) & 31];
            chars[19] = _encode32Chars[(int)id & 31];

            return new PooledCharBuffer(chars, reactIdLength);
        }
    }
}
