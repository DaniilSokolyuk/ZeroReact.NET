using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using ZeroReact.Utils;

namespace ZeroReact.Tests
{
    public partial class ArrayPooledTextWriterTests
    {
        static int[] iArrInvalidValues = new int[] { -1, -2, -100, -1000, -10000, -100000, -1000000, -10000000, -100000000, -1000000000, int.MinValue, short.MinValue };
        static int[] iArrLargeValues = new int[] { int.MaxValue, int.MaxValue - 1, int.MaxValue / 2, int.MaxValue / 10, int.MaxValue / 100 };
        static int[] iArrValidValues = new int[] { 10000, 100000, int.MaxValue / 2000, int.MaxValue / 5000, short.MaxValue };

        private static StringBuilder getSb()
        {
            var chArr = TestDataProvider.LargeData;
            var sb = new StringBuilder(40);
            for (int i = 0; i < chArr.Length; i++)
                sb.Append(chArr[i]);

            return sb;
        }

        [Fact]
        public static void Ctor()
        {
            ArrayPooledTextWriter sw = new ArrayPooledTextWriter();
            Assert.NotNull(sw);
        }

        [Fact]
        public static void SimpleWriter()
        {
            var sw = new ArrayPooledTextWriter();
            sw.Write(4);
            Assert.Equal("4", sw.ToString());
        }

        [Fact]
        public static void WriteArray()
        {
            var chArr = TestDataProvider.LargeData;
            var sb = getSb().ToString();

            ArrayPooledTextWriter sw = new ArrayPooledTextWriter();
            sw.Write(sb);

            var sr = new StringReader(sw.ToString());

            for (int i = 0; i < chArr.Length; i++)
            {
                int tmp = sr.Read();
                Assert.Equal((int)chArr[i], tmp);
            }
        }

        [Fact]
        public static void CantWriteNullArray()
        {
            var sw = new ArrayPooledTextWriter();
            Assert.Throws<ArgumentNullException>(() => sw.Write(null, 0, 0));
        }

        [Fact]
        public static void CantWriteNegativeOffset()
        {
            var sw = new ArrayPooledTextWriter();
            Assert.Throws<ArgumentOutOfRangeException>(() => sw.Write(new char[0], -1, 0));
        }

        [Fact]
        public static void CantWriteNegativeCount()
        {
            var sw = new ArrayPooledTextWriter();
            Assert.Throws<ArgumentOutOfRangeException>(() => sw.Write(new char[0], 0, -1));
        }

        [Fact]
        public static void WriteWithOffset()
        {
            ArrayPooledTextWriter sw = new ArrayPooledTextWriter();
            StringReader sr;

            var chArr = TestDataProvider.CharData;

            sw.Write(chArr, 2, 5);

            sr = new StringReader(sw.ToString());
            for (int i = 2; i < 7; i++)
            {
                int tmp = sr.Read();
                Assert.Equal((int)chArr[i], tmp);
            }
        }

        [Fact]
        public static void WriteWithLargeIndex()
        {
            for (int i = 0; i < iArrValidValues.Length; i++)
            {
                var sb = new StringBuilder(int.MaxValue / 2000).ToString();
                ArrayPooledTextWriter sw = new ArrayPooledTextWriter();
                sw.Write(sb);

                var chArr = new char[int.MaxValue / 2000];
                for (int j = 0; j < chArr.Length; j++)
                    chArr[j] = (char)(j % 256);
                sw.Write(chArr, iArrValidValues[i] - 1, 1);

                string strTemp = sw.ToString();
                Assert.Equal(1, strTemp.Length);
            }
        }

        [Fact]
        public static void WriteWithLargeCount()
        {
            for (int i = 0; i < iArrValidValues.Length; i++)
            {
                var sb = new StringBuilder(int.MaxValue / 2000).ToString();
                ArrayPooledTextWriter sw = new ArrayPooledTextWriter();
                sw.Write(sb);
                
                var chArr = new char[int.MaxValue / 2000];
                for (int j = 0; j < chArr.Length; j++)
                    chArr[j] = (char)(j % 256);

                sw.Write(chArr, 0, iArrValidValues[i]);

                string strTemp = sw.ToString();
                Assert.Equal(iArrValidValues[i], strTemp.Length);
            }
        }

        [Fact]
        public static void NewArrayPooledTextWriterIsEmpty()
        {
            var sw = new ArrayPooledTextWriter();
            Assert.Equal(string.Empty, sw.ToString());
        }

        [Fact]
        public static void NewArrayPooledTextWriterHasEmptyStringBuilder()
        {
            var sw = new ArrayPooledTextWriter();
            Assert.Equal(string.Empty, sw.ToString());
        }

        [Fact]
        public static void ToStringReturnsWrittenData()
        {
            var sb = getSb().ToString();
            ArrayPooledTextWriter sw = new ArrayPooledTextWriter();

            sw.Write(sb);

            Assert.Equal(sb, sw.ToString());
        }

        private static void ValidateDisposedExceptions(ArrayPooledTextWriter sw)
        {
            Assert.Throws<ObjectDisposedException>(() => { sw.Write('a'); });
            Assert.Throws<ObjectDisposedException>(() => { sw.Write(new char[10], 0, 1); });
            Assert.Throws<ObjectDisposedException>(() => { sw.Write("abc"); });
        }

        [Fact]
        public static void MiscWrites()
        {
            var sw = new ArrayPooledTextWriter();
            sw.Write('H');
            sw.Write("ello World!");

            Assert.Equal("Hello World!", sw.ToString());
        }

        [Fact]
        public static async Task MiscWritesAsync()
        {
            var sw = new ArrayPooledTextWriter();
            await sw.WriteAsync('H');
            await sw.WriteAsync(new char[] { 'e', 'l', 'l', 'o', ' ' });
            await sw.WriteAsync("World!");

            Assert.Equal("Hello World!", sw.ToString());
        }

        [Fact]
        public static async Task MiscWriteLineAsync()
        {
            var sw = new ArrayPooledTextWriter();
            await sw.WriteLineAsync('H');
            await sw.WriteLineAsync(new char[] { 'e', 'l', 'l', 'o' });
            await sw.WriteLineAsync("World!");

            Assert.Equal(
                string.Format("H{0}ello{0}World!{0}", Environment.NewLine),
                sw.ToString());
        }

        [Fact]
        public static void TestWriteObject()
        {
            var sw = new ArrayPooledTextWriter();
            sw.Write(new object());
            Assert.Equal("System.Object", sw.ToString());
        }


        [Fact]
        public static void TestWriteLineObject()
        {
            var sw = new ArrayPooledTextWriter();
            sw.WriteLine(new object());
            Assert.Equal("System.Object" + Environment.NewLine, sw.ToString());
        }

        [Fact]
        public static async Task TestWriteLineAsyncCharArray()
        {
            ArrayPooledTextWriter sw = new ArrayPooledTextWriter();
            await sw.WriteLineAsync(new char[] { 'H', 'e', 'l', 'l', 'o' });

            Assert.Equal("Hello" + Environment.NewLine, sw.ToString());
        }

        [Fact]
        public async Task NullNewLineAsync()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                string newLine;
                using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8, 16, true))
                {
                    newLine = sw.NewLine;
                    await sw.WriteLineAsync(default(string));
                    await sw.WriteLineAsync(default(string));
                }
                ms.Seek(0, SeekOrigin.Begin);
                using (StreamReader sr = new StreamReader(ms))
                {
                    Assert.Equal(newLine + newLine, await sr.ReadToEndAsync());
                }
            }
        }
    }
}
