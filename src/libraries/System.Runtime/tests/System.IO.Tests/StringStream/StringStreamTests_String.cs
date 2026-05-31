// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    /// <summary>
    /// Additional specific tests for StringStream with string beyond conformance tests.
    /// </summary>
    public class StringStreamTests_String
    {
        [Theory]
        [InlineData("Hello, World! ")]
        [InlineData("Unicode: 你好世界 🌍")]
        [InlineData("Multi\nLine\r\nText")]
        public async Task ReadsCorrectBytesForDifferentStrings(string input)
        {
            byte[] expectedBytes = Encoding.UTF8.GetBytes(input);
            var stream = new StringStream(input, Encoding.UTF8);

            byte[] actualBytes = new byte[expectedBytes.Length + 100];
            int totalRead = 0;
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(actualBytes.AsMemory(totalRead))) > 0)
            {
                totalRead += bytesRead;
            }

            Assert.Equal(expectedBytes.Length, totalRead);
            Assert.Equal(expectedBytes, actualBytes.AsSpan(0, totalRead).ToArray());
        }

        [Theory]
        [InlineData("ASCII text")]
        [InlineData("Ñoño español")]
        public async Task WorksWithDifferentEncodings(string input)
        {
            var encodings = new[] { Encoding.UTF8, Encoding.Unicode, Encoding.UTF32 };

            foreach (var encoding in encodings)
            {
                byte[] expectedBytes = encoding.GetBytes(input);
                var stream = new StringStream(input, encoding);

                byte[] actualBytes = new byte[expectedBytes.Length * 2];
                int totalRead = 0;
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(actualBytes.AsMemory(totalRead))) > 0)
                {
                    totalRead += bytesRead;
                }

                Assert.Equal(expectedBytes.Length, totalRead);
                Assert.Equal(expectedBytes, actualBytes.AsSpan(0, totalRead).ToArray());
            }
        }

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
        public void StreamCapabilities_ReturnsExpectedValues()
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
        public void UnsupportedOperations_ThrowNotSupportedException()
        {
            var stream = new StringStream("test", Encoding.UTF8);

            Assert.Throws<NotSupportedException>(() => stream.Length);
            Assert.Throws<NotSupportedException>(() => stream.Position);
            Assert.Throws<NotSupportedException>(() => stream.Position = 0);
            Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
            Assert.Throws<NotSupportedException>(() => stream.Write(new byte[1], 0, 1));
            Assert.Throws<NotSupportedException>(() => stream.SetLength(100));
        }

        [Fact]
        public async Task HandlesChunkedReading()
        {
            string largeString = new string('A', 10000);
            byte[] expectedBytes = Encoding.UTF8.GetBytes(largeString);
            var stream = new StringStream(largeString, Encoding.UTF8);

            byte[] actualBytes = new byte[expectedBytes.Length];
            int totalRead = 0;
            int chunkSize = 512;

            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(actualBytes.AsMemory(totalRead, Math.Min(chunkSize, expectedBytes.Length - totalRead)))) > 0)
            {
                totalRead += bytesRead;
            }

            Assert.Equal(expectedBytes.Length, totalRead);
            Assert.Equal(expectedBytes, actualBytes);
        }

        [Fact]
        public async Task ReadsWithExactBufferSizeMatch()
        {
            string input = new string('A', 4096);
            byte[] expectedBytes = Encoding.UTF8.GetBytes(input);
            var stream = new StringStream(input, Encoding.UTF8);

            byte[] buffer = new byte[4096];
            int bytesRead = await stream.ReadAsync(buffer);

            Assert.Equal(4096, bytesRead);
            Assert.Equal(expectedBytes, buffer);
        }

        [Fact]
        public async Task MultipleReadsEventuallyReturnZero()
        {
            var stream = new StringStream("small", Encoding.UTF8);
            byte[] buffer = new byte[100];

            int bytesRead = await stream.ReadAsync(buffer);
            Assert.Equal(5, bytesRead);

            int finalRead = await stream.ReadAsync(buffer);
            Assert.Equal(0, finalRead);
        }

        [Fact]
        public async Task SequentialReadAsync_WithSmallChunks_ReadsEntireStream()
        {
            string input = new string('A', 5000);
            byte[] expectedBytes = Encoding.UTF8.GetBytes(input);
            var stream = new StringStream(input, Encoding.UTF8);

            byte[] actualBytes = new byte[expectedBytes.Length];
            int totalBytesRead = 0;
            int chunkSize = 128;

            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(actualBytes.AsMemory(totalBytesRead, Math.Min(chunkSize, expectedBytes.Length - totalBytesRead)))) > 0)
            {
                totalBytesRead += bytesRead;
            }

            Assert.Equal(expectedBytes.Length, totalBytesRead);
            Assert.Equal(expectedBytes, actualBytes);
        }

        [Fact]
        public void DisposeRendersStreamUnreadable()
        {
            var stream = new StringStream("test", Encoding.UTF8);
            stream.Dispose();

            Assert.False(stream.CanRead);
            Assert.Throws<ObjectDisposedException>(() => stream.Read(new byte[1], 0, 1));
        }
    }
}
