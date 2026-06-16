// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public abstract class StringStreamTestBase
    {
        protected abstract Stream CreateStream(string input, Encoding encoding);
        protected abstract int ReadFromStream(Stream stream, byte[] buffer, int offset, int count);

        private int ReadToEnd(Stream stream, byte[] buffer)
        {
            int totalRead = 0;
            int bytesRead;
            while ((bytesRead = ReadFromStream(stream, buffer, totalRead, buffer.Length - totalRead)) > 0)
            {
                totalRead += bytesRead;
            }

            return totalRead;
        }

        [Theory]
        [InlineData("Hello, World! ")]
        [InlineData("Unicode: 你好世界 🌍")]
        [InlineData("Multi\nLine\r\nText")]
        public void ReadsCorrectBytesForDifferentStrings(string input)
        {
            byte[] expectedBytes = Encoding.UTF8.GetBytes(input);
            using Stream stream = CreateStream(input, Encoding.UTF8);

            byte[] actualBytes = new byte[expectedBytes.Length + 100];
            int totalRead = ReadToEnd(stream, actualBytes);

            Assert.Equal(expectedBytes.Length, totalRead);
            Assert.Equal(expectedBytes, actualBytes.AsSpan(0, totalRead).ToArray());
        }

        [Theory]
        [InlineData("ASCII text")]
        [InlineData("Ñoño español")]
        public void WorksWithDifferentEncodings(string input)
        {
            Encoding[] encodings = [Encoding.UTF8, Encoding.Unicode, Encoding.UTF32];

            foreach (Encoding encoding in encodings)
            {
                byte[] expectedBytes = encoding.GetBytes(input);
                using Stream stream = CreateStream(input, encoding);

                byte[] actualBytes = new byte[expectedBytes.Length * 2];
                int totalRead = ReadToEnd(stream, actualBytes);

                Assert.Equal(expectedBytes.Length, totalRead);
                Assert.Equal(expectedBytes, actualBytes.AsSpan(0, totalRead).ToArray());
            }
        }

        [Fact]
        public void HandlesEmptyInput()
        {
            using Stream stream = CreateStream("", Encoding.UTF8);
            byte[] buffer = new byte[10];
            int bytesRead = ReadFromStream(stream, buffer, 0, 10);
            Assert.Equal(0, bytesRead);
        }

        [Fact]
        public void HandlesSurrogatePairs()
        {
            string input = "😀😁😂🤣😃😄";
            byte[] expectedBytes = Encoding.UTF8.GetBytes(input);
            using Stream stream = CreateStream(input, Encoding.UTF8);

            byte[] actualBytes = new byte[expectedBytes.Length + 100];
            int totalRead = ReadToEnd(stream, actualBytes);

            Assert.Equal(expectedBytes.Length, totalRead);
            Assert.Equal(expectedBytes, actualBytes.AsSpan(0, totalRead).ToArray());
        }

        [Fact]
        public void MultiByteCharactersAcrossChunkBoundary()
        {
            string input = new string('A', 1023) + "你";
            byte[] expectedBytes = Encoding.UTF8.GetBytes(input);
            using Stream stream = CreateStream(input, Encoding.UTF8);

            byte[] actualBytes = new byte[expectedBytes.Length + 100];
            int totalRead = ReadToEnd(stream, actualBytes);

            Assert.Equal(expectedBytes.Length, totalRead);
            Assert.Equal(expectedBytes, actualBytes.AsSpan(0, totalRead).ToArray());
        }

        [Fact]
        public void HandlesChunkedReading()
        {
            string input = new string('A', 10000);
            byte[] expectedBytes = Encoding.UTF8.GetBytes(input);
            using Stream stream = CreateStream(input, Encoding.UTF8);

            byte[] actualBytes = new byte[expectedBytes.Length];
            int totalRead = 0;
            int chunkSize = 512;

            int bytesRead;
            while ((bytesRead = ReadFromStream(stream, actualBytes, totalRead,
                Math.Min(chunkSize, expectedBytes.Length - totalRead))) > 0)
            {
                totalRead += bytesRead;
            }

            Assert.Equal(expectedBytes.Length, totalRead);
            Assert.Equal(expectedBytes, actualBytes);
        }

        [Fact]
        public void MultipleReadsEventuallyReturnZero()
        {
            using Stream stream = CreateStream("small", Encoding.UTF8);
            byte[] buffer = new byte[100];

            int totalRead = ReadToEnd(stream, buffer);
            Assert.Equal(5, totalRead);

            int finalRead = ReadFromStream(stream, buffer, 0, buffer.Length);
            Assert.Equal(0, finalRead);
        }
    }
}
