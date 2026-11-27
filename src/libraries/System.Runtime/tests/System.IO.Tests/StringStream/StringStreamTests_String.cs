// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class StringStreamTests_String_Read : StringStreamTestBase
    {
        protected override Stream CreateStream(string input, Encoding encoding)
            => new StringStream(input, encoding);
        protected override int ReadFromStream(Stream stream, byte[] buffer, int offset, int count)
            => stream.Read(buffer, offset, count);
    }

    public class StringStreamTests_String_ReadSpan : StringStreamTestBase
    {
        protected override Stream CreateStream(string input, Encoding encoding)
            => new StringStream(input, encoding);
        protected override int ReadFromStream(Stream stream, byte[] buffer, int offset, int count)
            => stream.Read(buffer.AsSpan(offset, count));
    }

    public class StringStreamTests_String_ReadByte : StringStreamTestBase
    {
        protected override Stream CreateStream(string input, Encoding encoding)
            => new StringStream(input, encoding);
        protected override int ReadFromStream(Stream stream, byte[] buffer, int offset, int count)
        {
            int b = stream.ReadByte();
            if (b == -1) return 0;
            buffer[offset] = (byte)b;
            return 1;
        }
    }

    public class StringStreamTests_String_ReadAsyncMemory : StringStreamTestBase
    {
        protected override Stream CreateStream(string input, Encoding encoding)
            => new StringStream(input, encoding);
        protected override int ReadFromStream(Stream stream, byte[] buffer, int offset, int count)
            => stream.ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
    }

    public class StringStreamTests_String_ReadAsyncArray : StringStreamTestBase
    {
        protected override Stream CreateStream(string input, Encoding encoding)
            => new StringStream(input, encoding);
        protected override int ReadFromStream(Stream stream, byte[] buffer, int offset, int count)
            => stream.ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
    }

    public class StringStreamTests_String_Misc
    {
        [Fact]
        public void ThrowsOnNullString()
        {
            Assert.Throws<ArgumentNullException>(() => new StringStream((string)null!, Encoding.UTF8));
        }

        [Fact]
        public void ThrowsOnNullEncoding()
        {
            Assert.Throws<ArgumentNullException>(() => new StringStream("test", null!));
        }

        [Fact]
        public void StreamCapabilities()
        {
            using var stream = new StringStream("test", Encoding.UTF8);

            Assert.True(stream.CanRead);
            Assert.False(stream.CanSeek);
            Assert.False(stream.CanWrite);
            Assert.Throws<NotSupportedException>(() => stream.Length);
            Assert.Throws<NotSupportedException>(() => stream.Position);
            Assert.Throws<NotSupportedException>(() => stream.Position = 0);
            Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        }

        [Fact]
        public void EncodingPropertyReturnsCorrectEncoding()
        {
            var stream = new StringStream("test", Encoding.UTF32);
            Assert.Equal(Encoding.UTF32, stream.Encoding);
        }

        [Fact]
        public async Task CopyToAsync_HonorsCancellation()
        {
            using var stream = new StringStream("hello", Encoding.UTF8);
            using var destination = new MemoryStream();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<TaskCanceledException>(
                () => stream.CopyToAsync(destination, bufferSize: 81920, cts.Token));
        }

        [Theory]
        [InlineData(2, false)]
        [InlineData(4, false)]
        [InlineData(2, true)]
        [InlineData(4, true)]
        public void Read_EmptyEncoderReplacementFallback_ReturnsRemainingOutput(int invalidCharCount, bool useFastPath)
        {
            string input = new string('\uD800', invalidCharCount) + "X";
            Encoding encoding = CreateUtf8EncodingWithEmptyFallback();
            using var stream = new StringStream(input, encoding);
            // The one-byte cases force the streaming spillover path instead of the single-shot fast path.
            byte[] buffer = new byte[useFastPath ? encoding.GetMaxByteCount(input.Length) : 1];

            Assert.Equal(1, stream.Read(buffer));
            Assert.Equal((byte)'X', buffer[0]);
            Assert.Equal(0, stream.Read(buffer));
        }

        [Theory]
        [InlineData(2, false)]
        [InlineData(4, false)]
        [InlineData(2, true)]
        [InlineData(4, true)]
        public async Task ReadAsync_EmptyEncoderReplacementFallback_ReturnsRemainingOutput(int invalidCharCount, bool useFastPath)
        {
            string input = new string('\uD800', invalidCharCount) + "X";
            Encoding encoding = CreateUtf8EncodingWithEmptyFallback();
            using var stream = new StringStream(input, encoding);
            // The one-byte cases force the streaming spillover path instead of the single-shot fast path.
            byte[] buffer = new byte[useFastPath ? encoding.GetMaxByteCount(input.Length) : 1];

            Assert.Equal(1, await stream.ReadAsync(buffer.AsMemory()));
            Assert.Equal((byte)'X', buffer[0]);
            Assert.Equal(0, await stream.ReadAsync(buffer.AsMemory()));
        }

        [Fact]
        public void ReadByte_EmptyEncoderReplacementFallback_DoesNotReturnEofBeforeOutput()
        {
            using var stream = new StringStream("\uD800\uD800X", CreateUtf8EncodingWithEmptyFallback());

            Assert.Equal('X', stream.ReadByte());
            Assert.Equal(-1, stream.ReadByte());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Read_EmptyEncoderReplacementFallback_AllInputSkippedReturnsEof(bool useFastPath)
        {
            const string Input = "\uD800\uD800";
            Encoding encoding = CreateUtf8EncodingWithEmptyFallback();
            using var stream = new StringStream(Input, encoding);
            byte[] buffer = new byte[useFastPath ? encoding.GetMaxByteCount(Input.Length) : 1];

            Assert.Equal(0, stream.Read(buffer));
            Assert.Equal(0, stream.Read(buffer));
        }

        private static Encoding CreateUtf8EncodingWithEmptyFallback() =>
            Encoding.GetEncoding(
                "utf-8",
                new EncoderReplacementFallback(string.Empty),
                new DecoderReplacementFallback(string.Empty));
    }
}
