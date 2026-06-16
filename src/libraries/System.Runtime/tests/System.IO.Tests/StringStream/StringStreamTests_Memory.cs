// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class StringStreamTests_Memory_Read : StringStreamTestBase
    {
        protected override Stream CreateStream(string input, Encoding encoding)
            => new StringStream(input.AsMemory(), encoding);
        protected override int ReadFromStream(Stream stream, byte[] buffer, int offset, int count)
            => stream.Read(buffer, offset, count);
    }

    public class StringStreamTests_Memory_ReadSpan : StringStreamTestBase
    {
        protected override Stream CreateStream(string input, Encoding encoding)
            => new StringStream(input.AsMemory(), encoding);
        protected override int ReadFromStream(Stream stream, byte[] buffer, int offset, int count)
            => stream.Read(buffer.AsSpan(offset, count));
    }

    public class StringStreamTests_Memory_ReadByte : StringStreamTestBase
    {
        protected override Stream CreateStream(string input, Encoding encoding)
            => new StringStream(input.AsMemory(), encoding);
        protected override int ReadFromStream(Stream stream, byte[] buffer, int offset, int count)
        {
            int b = stream.ReadByte();
            if (b == -1) return 0;
            buffer[offset] = (byte)b;
            return 1;
        }
    }

    public class StringStreamTests_Memory_ReadAsyncMemory : StringStreamTestBase
    {
        protected override Stream CreateStream(string input, Encoding encoding)
            => new StringStream(input.AsMemory(), encoding);
        protected override int ReadFromStream(Stream stream, byte[] buffer, int offset, int count)
            => stream.ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
    }

    public class StringStreamTests_Memory_ReadAsyncArray : StringStreamTestBase
    {
        protected override Stream CreateStream(string input, Encoding encoding)
            => new StringStream(input.AsMemory(), encoding);
        protected override int ReadFromStream(Stream stream, byte[] buffer, int offset, int count)
            => stream.ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
    }

    public class StringStreamTests_Memory_Misc
    {
        [Fact]
        public async Task WorksWithMemorySlice()
        {
            string source = "0123456789ABCDEFGHIJ";
            ReadOnlyMemory<char> slice = source.AsMemory(5, 10);

            byte[] expectedBytes = Encoding.UTF8.GetBytes("56789ABCDE");
            using var stream = new StringStream(slice, Encoding.UTF8);

            byte[] actualBytes = new byte[expectedBytes.Length + 10];
            int totalRead = 0;
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(actualBytes.AsMemory(totalRead))) > 0)
                totalRead += bytesRead;

            Assert.Equal(expectedBytes.Length, totalRead);
            Assert.Equal(expectedBytes, actualBytes.AsSpan(0, totalRead).ToArray());
        }

        [Fact]
        public async Task WorksWithCharArray()
        {
            char[] charArray = ['H', 'e', 'l', 'l', 'o'];
            var memory = new ReadOnlyMemory<char>(charArray);

            byte[] expectedBytes = Encoding.UTF8.GetBytes("Hello");
            using var stream = new StringStream(memory, Encoding.UTF8);

            byte[] actualBytes = new byte[expectedBytes.Length + 10];
            int totalRead = 0;
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(actualBytes.AsMemory(totalRead))) > 0)
                totalRead += bytesRead;

            Assert.Equal(expectedBytes.Length, totalRead);
            Assert.Equal(expectedBytes, actualBytes.AsSpan(0, totalRead).ToArray());
        }

        [Theory]
        [InlineData("Hello")]
        [InlineData("Unicode: 你好")]
        [InlineData("Emoji: 😀")]
        public async Task ProducesSameOutputAsStringOverload(string input)
        {
            using var memoryStream = new StringStream(input.AsMemory(), Encoding.UTF8);
            using var stringStream = new StringStream(input, Encoding.UTF8);

            byte[] memoryResult = new byte[1000];
            byte[] stringResult = new byte[1000];

            int memoryBytesRead = await memoryStream.ReadAsync(memoryResult);
            int stringBytesRead = await stringStream.ReadAsync(stringResult);

            Assert.Equal(stringBytesRead, memoryBytesRead);
            Assert.Equal(
                stringResult.AsSpan(0, stringBytesRead).ToArray(),
                memoryResult.AsSpan(0, memoryBytesRead).ToArray());
        }

        [Fact]
        public void UnsupportedOperationsThrow()
        {
            var stream = new StringStream("test".AsMemory(), Encoding.UTF8);

            Assert.Throws<NotSupportedException>(() => stream.Length);
            Assert.Throws<NotSupportedException>(() => stream.Position);
            Assert.Throws<NotSupportedException>(() => stream.Position = 0);
            Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
            Assert.Throws<NotSupportedException>(() => stream.Write(new byte[1], 0, 1));
            Assert.Throws<NotSupportedException>(() => stream.SetLength(100));
        }

        [Fact]
        public void DisposeIsIdempotent()
        {
            var stream = new StringStream("test".AsMemory(), Encoding.UTF8);

            stream.Dispose();
            Assert.False(stream.CanRead);
            Assert.Throws<ObjectDisposedException>(() => stream.Read(new byte[10], 0, 10));

            stream.Dispose();
            stream.Dispose();
        }

        [Fact]
        public void TruncatedSurrogatePairProducesReplacementChar()
        {
            // "🌍" is U+1F30D = surrogate pair (0xD83C, 0xDF0D)
            // Slice after the high surrogate to create an unpaired surrogate
            string emoji = "A\U0001F30D";
            ReadOnlyMemory<char> truncated = emoji.AsMemory(0, 2); // 'A' + high surrogate only

            using var stream = new StringStream(truncated, Encoding.UTF8);
            byte[] buffer = new byte[64];
            int totalRead = 0;
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, totalRead, buffer.Length - totalRead)) > 0)
                totalRead += bytesRead;

            // Encoder should produce U+FFFD replacement character for the unpaired surrogate
            byte[] expected = Encoding.UTF8.GetBytes("A\uFFFD");
            Assert.Equal(expected, buffer.AsSpan(0, totalRead).ToArray());
        }
    }
}
