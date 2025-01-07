// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class StringReaderTests
    {
        [Fact]
        public static void StringReaderWithNullString()
        {
            Assert.Throws<ArgumentNullException>(() => new StringReader(null));
        }

        [Fact]
        public static void StringReaderWithEmptyString()
        {
            // [] Check vanilla construction
            //-----------------------------------------------------------
            StringReader sr = new StringReader(string.Empty);
            Assert.Equal(string.Empty, sr.ReadToEnd());
        }

        [Fact]
        public static void StringReaderWithGenericString()
        {
            // [] Another vanilla construction
            //-----------------------------------------------------------

            StringReader sr = new StringReader("Hello\0World");
            Assert.Equal("Hello\0World", sr.ReadToEnd());
        }

        [Fact]
        public static void ReadEmptyString() {
            StringReader sr = new StringReader(string.Empty);
            Assert.Equal(-1, sr.Read());

        }

        [Fact]
        public static void ReadString() {
            string str1 = "Hello\0\t\v   \\ World";
            StringReader sr = new StringReader(str1);
            for (int i = 0; i < str1.Length; i++)
            {
                Assert.Equal((int)str1[i], sr.Read());
            }
        }

        [Fact]
        public static void ReadLine()
        {
            string str1 = "Hello\0\t\v   \\ World";
            string str2 = str1 + Environment.NewLine + str1;

            using (StringReader sr = new StringReader(str1))
            {
                Assert.Same(str1, sr.ReadLine());
            }
            using (StringReader sr = new StringReader(str2))
            {
                Assert.Equal(str1, sr.ReadLine());
                Assert.Equal(str1, sr.ReadLine());
            }
        }

        [Fact]
        public static async Task ReadLineAsync()
        {
            string str1 = "Hello\0\t\v   \\ World";
            string str2 = str1 + Environment.NewLine + str1;

            using (StringReader sr = new StringReader(str1))
            {
                Assert.Equal(str1, await sr.ReadLineAsync());
            }
            using (StringReader sr = new StringReader(str2))
            {
                Assert.Equal(str1, await sr.ReadLineAsync(default));
                Assert.Equal(str1, await sr.ReadLineAsync(default));
            }
        }

        [Fact]
        public static void ReadPseudoRandomString()
        {
            string str1 = string.Empty;
            Random r = new Random(-55);
            for (int i = 0; i < 5000; i++)
                str1 += (char)r.Next(0, 255);

            StringReader sr = new StringReader(str1);
            for (int i = 0; i < str1.Length; i++)
            {
                Assert.Equal((int)str1[i], sr.Read());
            }
        }

        [Fact]
        public static void PeekEmptyString()
        {
            StringReader sr = new StringReader(string.Empty);
            Assert.Equal(-1, sr.Peek());

        }

        [Fact]
        public static void PeekString()
        {
            string str1 = "Hello\0\t\v   \\ World";
            StringReader sr = new StringReader(str1);
            for (int i = 0; i < str1.Length; i++)
            {
                int test = sr.Peek();
                sr.Read();
                Assert.Equal((int)str1[i], test);
            }

        }

        [Fact]
        public static void PeekPseudoRandomString()
        {
            string str1 = string.Empty;
            Random r = new Random(-55);
            for (int i = 0; i < 5000; i++)
                str1 += (char)r.Next(0, 255);

            StringReader sr = new StringReader(str1);
            for (int i = 0; i < str1.Length; i++)
            {
                int test = sr.Peek();
                sr.Read();
                Assert.Equal((int)str1[i], test);
            }
        }

        [Fact]
        public static void ReadToEndEmptyString()
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////

            StringReader sr;

            sr = new StringReader(string.Empty);
            Assert.Equal(string.Empty, sr.ReadToEnd());

        }

        [Fact]
        public static void ReadToEndString() {
            string str1 = "Hello\0\t\v   \\ World";
            StringReader sr = new StringReader(str1);
            Assert.Equal(str1, sr.ReadToEnd());
        }

        [Fact]
        public static void ReadToEndPseudoRandom() {
            // [] Try with large random strings
            //-----------------------------------------------------------
            string str1 = string.Empty;
            Random r = new Random(-55);
            for (int i = 0; i < 10000; i++)
                str1 += (char)r.Next(0, 255);

            StringReader sr = new StringReader(str1);
            Assert.Equal(str1, sr.ReadToEnd());
        }

        [Fact]
        public static async Task ReadToEndAsyncString()
        {
            string str1 = "Hello\0\t\v   \\ World";
            StringReader sr = new StringReader(str1);
            Assert.Equal(str1, await sr.ReadToEndAsync(default));
        }

        [Fact]
        public static void Closed_DisposedExceptions()
        {
            StringReader sr = new StringReader("abcdefg");
            sr.Close();
            ValidateDisposedExceptions(sr);
        }

        [Fact]
        public static void Disposed_DisposedExceptions()
        {
            StringReader sr = new StringReader("abcdefg");
            sr.Dispose();
            ValidateDisposedExceptions(sr);
        }

        [Fact]
        public void ReadSpan_Success()
        {
            string input = "abcdef";
            var reader = new StringReader(input);
            Span<char> s = new char[2];

            Assert.Equal(2, reader.Read(s));
            Assert.Equal("ab", new string(s.ToArray()));

            Assert.Equal(1, reader.Read(s.Slice(0, 1)));
            Assert.Equal("cb", new string(s.ToArray()));

            Assert.Equal(2, reader.Read(s));
            Assert.Equal("de", new string(s.ToArray()));

            Assert.Equal(1, reader.Read(s));
            Assert.Equal("f", new string(s.Slice(0, 1).ToArray()));

            Assert.Equal(0, reader.Read(s));
        }

        [Fact]
        public void ReadBlockSpan_Success()
        {
            string input = "abcdef";
            var reader = new StringReader(input);
            Span<char> s = new char[2];

            Assert.Equal(2, reader.ReadBlock(s));
            Assert.Equal("ab", new string(s.ToArray()));

            Assert.Equal(1, reader.ReadBlock(s.Slice(0, 1)));
            Assert.Equal("cb", new string(s.ToArray()));

            Assert.Equal(2, reader.ReadBlock(s));
            Assert.Equal("de", new string(s.ToArray()));

            Assert.Equal(1, reader.ReadBlock(s));
            Assert.Equal("f", new string(s.Slice(0, 1).ToArray()));

            Assert.Equal(0, reader.ReadBlock(s));
        }

        [Fact]
        public async Task ReadMemoryAsync_Success()
        {
            string input = "abcdef";
            var reader = new StringReader(input);
            Memory<char> m = new char[2];

            Assert.Equal(2, await reader.ReadAsync(m));
            Assert.Equal("ab", new string(m.ToArray()));

            Assert.Equal(1, await reader.ReadAsync(m.Slice(0, 1)));
            Assert.Equal("cb", new string(m.ToArray()));

            Assert.Equal(2, await reader.ReadAsync(m));
            Assert.Equal("de", new string(m.ToArray()));

            Assert.Equal(1, await reader.ReadAsync(m));
            Assert.Equal("f", new string(m.Slice(0, 1).ToArray()));

            Assert.Equal(0, await reader.ReadAsync(m));
        }

        [Fact]
        public async Task ReadBlockMemoryAsync_Success()
        {
            string input = "abcdef";
            var reader = new StringReader(input);
            Memory<char> m = new char[2];

            Assert.Equal(2, await reader.ReadBlockAsync(m));
            Assert.Equal("ab", new string(m.ToArray()));

            Assert.Equal(1, await reader.ReadBlockAsync(m.Slice(0, 1)));
            Assert.Equal("cb", new string(m.ToArray()));

            Assert.Equal(2, await reader.ReadBlockAsync(m));
            Assert.Equal("de", new string(m.ToArray()));

            Assert.Equal(1, await reader.ReadBlockAsync(m));
            Assert.Equal("f", new string(m.Slice(0, 1).ToArray()));

            Assert.Equal(0, await reader.ReadBlockAsync(m));
        }

        [Fact]
        public void Disposed_ThrowsException()
        {
            var reader = new StringReader("abc");
            reader.Dispose();

            Assert.Throws<ObjectDisposedException>(() => reader.Read(Span<char>.Empty));
            Assert.Throws<ObjectDisposedException>(() => reader.ReadBlock(Span<char>.Empty));
            Assert.Throws<ObjectDisposedException>(() => { reader.ReadAsync(Memory<char>.Empty); });
            Assert.Throws<ObjectDisposedException>(() => { reader.ReadBlockAsync(Memory<char>.Empty); });
        }

        [Fact]
        public async Task Precanceled_ThrowsException()
        {
            var reader = new StringReader("abc");

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.ReadAsync(Memory<char>.Empty, new CancellationToken(true)).AsTask());
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.ReadBlockAsync(Memory<char>.Empty, new CancellationToken(true)).AsTask());
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.ReadLineAsync(new CancellationToken(true)).AsTask());
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.ReadToEndAsync(new CancellationToken(true)));
        }

        private static void ValidateDisposedExceptions(StringReader sr)
        {
            Assert.Throws<ObjectDisposedException>(() => { sr.Peek(); });
            Assert.Throws<ObjectDisposedException>(() => { sr.Read(); });
            Assert.Throws<ObjectDisposedException>(() => { sr.Read(new char[10], 0 , 1); });
        }
    }
}
