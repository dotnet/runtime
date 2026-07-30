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
            var stream = new StringStream("test", Encoding.UTF8);
            Assert.True(stream.CanRead);
            Assert.False(stream.CanSeek);
            Assert.False(stream.CanWrite);
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

    }
}
