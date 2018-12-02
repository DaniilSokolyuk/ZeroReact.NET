using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JavaScriptEngineSwitcher.ChakraCore.JsRt;

namespace ZeroReact.Utils
{
    public class ArrayPooledTextWriter : TextWriter
    {
        private readonly ArrayPool<char> _pool = ArrayPool<char>.Shared;

        public ArrayPooledTextWriter(int pageSize = 4096)
        {
            PageSize = pageSize;
        }

        private readonly int PageSize;

        private int _charIndex;

        private List<char[]> pages { get; } = new List<char[]>();

        private char[] CurrentPage { get; set; }

        public int Length
        {
            get
            {
                var length = _charIndex;
                for (var i = 0; i < pages.Count - 1; i++)
                {
                    length += pages[i].Length;
                }

                return length;
            }
        }

        public void WriteTo(TextWriter writer)
        {
            var length = Length;
            if (length == 0)
            {
                return;
            }

            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                var pageLength = Math.Min(length, page.Length);
                writer.Write(page, index: 0, count: pageLength);
                length -= pageLength;
            }
        }

        public override Encoding Encoding { get; }

        public override void Write(char value)
        {
            var page = GetCurrentPage();
            page[_charIndex++] = value;
        }

        public override void Write(char[] buffer)
        {
            if (buffer == null)
            {
                return;
            }

            Write(buffer, 0, buffer.Length);
        }

        public override void Write(string value)
        {
            if (value == null)
            {
                return;
            }

            var index = 0;
            var count = value.Length;

            while (count > 0)
            {
                var page = GetCurrentPage();
                var copyLength = Math.Min(count, page.Length - _charIndex);

                value.CopyTo(
                    index,
                    page,
                    _charIndex,
                    copyLength);

                _charIndex += copyLength;
                index += copyLength;

                count -= copyLength;
            }
        }

        public override void Write(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            while (count > 0)
            {
                var page = GetCurrentPage();
                var copyLength = Math.Min(count, page.Length - _charIndex);

                Array.Copy(
                    buffer,
                    index,
                    page,
                    _charIndex,
                    copyLength);

                _charIndex += copyLength;
                index += copyLength;
                count -= copyLength;
            }
        }

        public void Clear()
        {
            for (var i = pages.Count - 1; i > 0; i--)
            {
                var page = pages[i];

                try
                {
                    pages.RemoveAt(i);
                }
                finally
                {
                    _pool.Return(page);
                }
            }

            _charIndex = 0;
            CurrentPage = pages.Count > 0 ? pages[0] : null;
        }

        private char[] GetCurrentPage()
        {
            if (CurrentPage == null || _charIndex == CurrentPage.Length)
            {
                CurrentPage = NewPage();
                _charIndex = 0;
            }

            return CurrentPage;
        }

        private char[] NewPage()
        {
            char[] page = null;
            try
            {
                page = _pool.Rent(PageSize);
                pages.Add(page);
            }
            catch when (page != null)
            {
                _pool.Return(page);
                throw;
            }

            return page;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            for (var i = 0; i < pages.Count; i++)
            {
                _pool.Return(pages[i]);
            }

            pages.Clear();
        }

        public PooledCharBuffer ToPooledCharBuffer()
        {
            var length = Length;

            if (length == 0)
            {
                return new PooledCharBuffer(Array.Empty<char>(), 0);
            }

            char[] sb = _pool.Rent(length);

            int index = 0;

            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                var pageLength = Math.Min(length, page.Length);

                Array.Copy(page, 0, sb, index, pageLength);

                length -= pageLength;
                index += pageLength;
            }

            return new PooledCharBuffer(sb, index);
        }

        public override string ToString()
        {
            using (var buffer = ToPooledCharBuffer())
            {
                return new string(buffer.Array, 0, buffer.Length);
            }
        }
    }
}