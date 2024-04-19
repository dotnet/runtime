// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class WriteTests
    {
        private const string LowerAlpha = "abcdefghijklmnopqrstuvwxyz";

        protected virtual Stream CreateStream()
        {
            return new MemoryStream();
        }

        [Fact]
        public void WriteChars()
        {
            char[] chArr = TestDataProvider.CharData;

            // [] Write a wide variety of characters and read them back

            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);
            StreamReader sr;

            for (int i = 0; i < chArr.Length; i++)
                sw.Write(chArr[i]);
            sw.Flush();
            ms.Position = 0;
            sr = new StreamReader(ms);

            for (int i = 0; i < chArr.Length; i++)
            {
                Assert.Equal((int)chArr[i], sr.Read());
            }
        }

        [Fact]
        public void NullArray()
        {
            // [] Exception for null array
            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);

            Assert.Throws<ArgumentNullException>(() => sw.Write(null, 0, 0));
            sw.Dispose();
        }

        [Fact]
        public void NegativeOffset()
        {
            char[] chArr = TestDataProvider.CharData;

            // [] Exception if offset is negative
            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);

            Assert.Throws<ArgumentOutOfRangeException>(() => sw.Write(chArr, -1, 0));
            sw.Dispose();
        }

        [Fact]
        public void NegativeCount()
        {
            char[] chArr = TestDataProvider.CharData;

            // [] Exception if count is negative
            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);

            Assert.Throws<ArgumentOutOfRangeException>(() => sw.Write(chArr, 0, -1));
            sw.Dispose();
        }

        [Fact]
        public void WriteCustomLengthStrings()
        {
            char[] chArr = TestDataProvider.CharData;

            // [] Write some custom length strings
            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);
            StreamReader sr;

            sw.Write(chArr, 2, 5);
            sw.Flush();
            ms.Position = 0;
            sr = new StreamReader(ms);
            int tmp = 0;
            for (int i = 2; i < 7; i++)
            {
                tmp = sr.Read();
                Assert.Equal((int)chArr[i], tmp);
            }
            ms.Dispose();
        }

        [Fact]
        public void WriteToStreamWriter()
        {
            char[] chArr = TestDataProvider.CharData;
            // [] Just construct a streamwriter and write to it
            //-------------------------------------------------
            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);
            StreamReader sr;

            sw.Write(chArr, 0, chArr.Length);
            sw.Flush();
            ms.Position = 0;
            sr = new StreamReader(ms);

            for (int i = 0; i < chArr.Length; i++)
            {
                Assert.Equal((int)chArr[i], sr.Read());
            }
            ms.Dispose();
        }

        [Fact]
        public void TestWritingPastEndOfArray()
        {
            char[] chArr = TestDataProvider.CharData;
            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);

            AssertExtensions.Throws<ArgumentException>(null, () => sw.Write(chArr, 1, chArr.Length));
            sw.Dispose();
        }

        [Fact]
        public void VerifyWrittenString()
        {
            char[] chArr = TestDataProvider.CharData;
            // [] Write string with wide selection of characters and read it back

            StringBuilder sb = new StringBuilder(40);
            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);
            StreamReader sr;

            for (int i = 0; i < chArr.Length; i++)
                sb.Append(chArr[i]);

            sw.Write(sb.ToString());
            sw.Flush();
            ms.Position = 0;
            sr = new StreamReader(ms);

            for (int i = 0; i < chArr.Length; i++)
            {
                Assert.Equal((int)chArr[i], sr.Read());
            }
        }

        [Fact]
        public void NullStreamThrows()
        {
            // [] Null string should write nothing

            Stream ms = CreateStream();
            StreamWriter sw = new StreamWriter(ms);
            sw.Write((string)null);
            sw.Flush();
            Assert.Equal(0, ms.Length);
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

        [Fact]
        public void Write_EmptySpan_WritesNothing()
        {
            using (var s = new MemoryStream())
            using (var writer = new StreamWriter(s))
            {
                writer.Write(ReadOnlySpan<char>.Empty);
                writer.Flush();
                Assert.Equal(0, s.Position);
            }
        }

        [Fact]
        public void WriteLine_EmptySpan_WritesNewLine()
        {
            using (var s = new MemoryStream())
            using (var writer = new StreamWriter(s))
            {
                writer.WriteLine(ReadOnlySpan<char>.Empty);
                writer.Flush();
                Assert.Equal(Environment.NewLine.Length, s.Position);
            }
        }

        [Fact]
        public async Task WriteAsync_EmptyMemory_WritesNothing()
        {
            using (var s = new MemoryStream())
            using (var writer = new StreamWriter(s))
            {
                await writer.WriteAsync(ReadOnlyMemory<char>.Empty);
                await writer.FlushAsync();
                Assert.Equal(0, s.Position);
            }
        }

        [Fact]
        public async Task WriteLineAsync_EmptyMemory_WritesNothing()
        {
            using (var s = new MemoryStream())
            using (var writer = new StreamWriter(s))
            {
                await writer.WriteLineAsync(ReadOnlyMemory<char>.Empty);
                await writer.FlushAsync();
                Assert.Equal(Environment.NewLine.Length, s.Position);
            }
        }

        [Theory]
        [InlineData(1, 1, 1, false)]
        [InlineData(100, 1, 100, false)]
        [InlineData(100, 10, 3, false)]
        [InlineData(1, 1, 1, true)]
        [InlineData(100, 1, 100, true)]
        [InlineData(100, 10, 3, true)]
        public void Write_Span_WritesExpectedData(int length, int writeSize, int writerBufferSize, bool autoFlush)
        {
            using (var s = new MemoryStream())
            using (var writer = new StreamWriter(s, Encoding.ASCII, writerBufferSize) { AutoFlush = autoFlush })
            {
                var rand = new Random(42);
                char[] data = rand.GetItems<char>(LowerAlpha, length);

                Span<char> source = data;
                while (source.Length > 0)
                {
                    int n = Math.Min(source.Length, writeSize);
                    writer.Write(source.Slice(0, n));
                    source = source.Slice(n);
                }

                writer.Flush();

                Assert.Equal(data, s.ToArray().Select(b => (char)b));
            }
        }

        [Theory]
        [InlineData(1, 1, 1, false)]
        [InlineData(100, 1, 100, false)]
        [InlineData(100, 10, 3, false)]
        [InlineData(1, 1, 1, true)]
        [InlineData(100, 1, 100, true)]
        [InlineData(100, 10, 3, true)]
        public async Task Write_Memory_WritesExpectedData(int length, int writeSize, int writerBufferSize, bool autoFlush)
        {
            using (var s = new MemoryStream())
            using (var writer = new StreamWriter(s, Encoding.ASCII, writerBufferSize) { AutoFlush = autoFlush })
            {
                var rand = new Random(42);
                char[] data = rand.GetItems<char>(LowerAlpha, length);

                ReadOnlyMemory<char> source = data;
                while (source.Length > 0)
                {
                    int n = Math.Min(source.Length, writeSize);
                    await writer.WriteAsync(source.Slice(0, n));
                    source = source.Slice(n);
                }

                await writer.FlushAsync();

                Assert.Equal(data, s.ToArray().Select(b => (char)b));
            }
        }

        [Theory]
        [InlineData(1, 1, 1, false)]
        [InlineData(100, 1, 100, false)]
        [InlineData(100, 10, 3, false)]
        [InlineData(1, 1, 1, true)]
        [InlineData(100, 1, 100, true)]
        [InlineData(100, 10, 3, true)]
        public void WriteLine_Span_WritesExpectedData(int length, int writeSize, int writerBufferSize, bool autoFlush)
        {
            using (var s = new MemoryStream())
            using (var writer = new StreamWriter(s, Encoding.ASCII, writerBufferSize) { AutoFlush = autoFlush })
            {
                var rand = new Random(42);
                char[] data = rand.GetItems<char>(LowerAlpha, length);

                Span<char> source = data;
                while (source.Length > 0)
                {
                    int n = Math.Min(source.Length, writeSize);
                    writer.WriteLine(source.Slice(0, n));
                    source = source.Slice(n);
                }

                writer.Flush();

                Assert.Equal(length + (Environment.NewLine.Length * (length / writeSize)), s.Length);
            }
        }

        [Theory]
        [InlineData(1, 1, 1, false)]
        [InlineData(100, 1, 100, false)]
        [InlineData(100, 10, 3, false)]
        [InlineData(1, 1, 1, true)]
        [InlineData(100, 1, 100, true)]
        [InlineData(100, 10, 3, true)]
        public async Task WriteLineAsync_Memory_WritesExpectedData(int length, int writeSize, int writerBufferSize, bool autoFlush)
        {
            using (var s = new MemoryStream())
            using (var writer = new StreamWriter(s, Encoding.ASCII, writerBufferSize) { AutoFlush = autoFlush })
            {
                var rand = new Random(42);
                char[] data = rand.GetItems<char>(LowerAlpha, length);

                ReadOnlyMemory<char> source = data;
                while (source.Length > 0)
                {
                    int n = Math.Min(source.Length, writeSize);
                    await writer.WriteLineAsync(source.Slice(0, n));
                    source = source.Slice(n);
                }

                await writer.FlushAsync();

                Assert.Equal(length + (Environment.NewLine.Length * (length / writeSize)), s.Length);
            }
        }

        [Fact]
        public async Task WriteAsync_Precanceled_ThrowsCancellationException()
        {
            using (var writer = new StreamWriter(Stream.Null))
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => writer.WriteAsync(ReadOnlyMemory<char>.Empty, new CancellationToken(true)));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => writer.WriteLineAsync(ReadOnlyMemory<char>.Empty, new CancellationToken(true)));
            }
        }

        [Fact]
        public void StreamWriter_WithOptionalArguments_NoExceptions()
        {
            Encoding UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

            // check enabled leaveOpen and default encoding
            using (var tempStream = new MemoryStream())
            {
                using (var sw = new StreamWriter(tempStream, leaveOpen: true))
                {
                    Assert.Equal(UTF8NoBOM, sw.Encoding);
                }
                Assert.True(tempStream.CanRead);
            }

            // check null encoding, default encoding, default leaveOpen
            using (var tempStream = new MemoryStream())
            {
                using (var sw = new StreamWriter(tempStream, encoding: null))
                {
                    Assert.Equal(UTF8NoBOM, sw.Encoding);
                }
                Assert.False(tempStream.CanRead);
            }

            // check bufferSize, default BOM, default leaveOpen
            using (var tempStream = new MemoryStream())
            {
                using (var sw = new StreamWriter(tempStream, bufferSize: -1))
                {
                    Assert.Equal(UTF8NoBOM, sw.Encoding);
                }
                Assert.False(tempStream.CanRead);
            }
        }

        [Fact]
        public async Task StreamWriter_WriteAsync_EmitBOMAndFlushDataWhenBufferIsFull()
        {
            Encoding UTF8BOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true, throwOnInvalidBytes: true);

            using (var s = new MemoryStream())
            using (var writer = new StreamWriter(s, UTF8BOM, 4))
            {
                await writer.WriteAsync("abcdefg");
                await writer.FlushAsync();

                Assert.Equal(10, s.Length); // BOM (3) + string value (7)
            }
        }
    }
}
