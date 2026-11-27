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
            AssertExtensions.SequenceEqual(expectedBytes, (ReadOnlySpan<byte>)actualBytes.AsSpan(0, totalRead));
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
                AssertExtensions.SequenceEqual(expectedBytes, (ReadOnlySpan<byte>)actualBytes.AsSpan(0, totalRead));
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
            AssertExtensions.SequenceEqual(expectedBytes, (ReadOnlySpan<byte>)actualBytes.AsSpan(0, totalRead));
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
            AssertExtensions.SequenceEqual(expectedBytes, (ReadOnlySpan<byte>)actualBytes.AsSpan(0, totalRead));
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
            AssertExtensions.SequenceEqual(expectedBytes, (ReadOnlySpan<byte>)actualBytes);
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

        [Fact]
        public void PendingBytesDrainAcrossSingleByteReads()
        {
            // '你' encodes to 3 UTF-8 bytes (0xE4 0xBD 0xA0). A 1-byte buffer forces
            // the encoder spillover path; subsequent reads must drain _pendingBytes
            // before encoding the next character.
            string input = "你";
            byte[] expected = Encoding.UTF8.GetBytes(input);

            using Stream stream = CreateStream(input, Encoding.UTF8);
            byte[] buffer = new byte[1];

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(1, ReadFromStream(stream, buffer, 0, 1));
                Assert.Equal(expected[i], buffer[0]);
            }

            Assert.Equal(0, ReadFromStream(stream, buffer, 0, 1));
        }

        [Fact]
        public void Read_FastPathGuard_PreventsOverflowFromGetMaxByteCount()
        {
            // The fast-path precondition must skip GetMaxByteCount(_text.Length) when it would overflow.
            using Stream stream = CreateStream("hello", new OverflowingEncoding());

            Exception ex = Record.Exception(() => ReadFromStream(stream, new byte[16], 0, 16));
            Assert.False(ex is OverflowException, ex?.ToString());
        }

        private sealed class OverflowingEncoding : Encoding
        {
            public override int GetByteCount(char[] chars, int index, int count)
                => UTF8.GetByteCount(chars, index, count);

            public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
                => UTF8.GetBytes(chars, charIndex, charCount, bytes, byteIndex);

            public override int GetMaxByteCount(int charCount) => charCount switch
            {
                1 => int.MaxValue,
                2 => 8,
                _ => checked((charCount + 1) * int.MaxValue),
            };

            public override int GetCharCount(byte[] bytes, int index, int count)
                => throw new NotImplementedException();

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
                => throw new NotImplementedException();

            public override int GetMaxCharCount(int byteCount)
                => throw new NotImplementedException();
        }
    }
}
