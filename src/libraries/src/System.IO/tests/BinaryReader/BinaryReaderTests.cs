// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public partial class BinaryReaderTests
    {
        protected virtual Stream CreateStream()
        {
            return new MemoryStream();
        }

        [Fact]
        public void BinaryReader_DisposeTests()
        {
            // Disposing multiple times should not throw an exception
            using (Stream memStream = CreateStream())
            using (BinaryReader binaryReader = new BinaryReader(memStream))
            {
                binaryReader.Dispose();
                binaryReader.Dispose();
                binaryReader.Dispose();
            }
        }

        [Fact]
        public void BinaryReader_CloseTests()
        {
            // Closing multiple times should not throw an exception
            using (Stream memStream = CreateStream())
            using (BinaryReader binaryReader = new BinaryReader(memStream))
            {
                binaryReader.Close();
                binaryReader.Close();
                binaryReader.Close();
            }
        }

        [Fact]
        public void BinaryReader_DisposeTests_Negative()
        {
            using (Stream memStream = CreateStream())
            {
                BinaryReader binaryReader = new BinaryReader(memStream);
                binaryReader.Dispose();
                ValidateDisposedExceptions(binaryReader);
            }
        }

        [Fact]
        public void BinaryReader_CloseTests_Negative()
        {
            using (Stream memStream = CreateStream())
            {
                BinaryReader binaryReader = new BinaryReader(memStream);
                binaryReader.Close();
                ValidateDisposedExceptions(binaryReader);
            }
        }

        private void ValidateDisposedExceptions(BinaryReader binaryReader)
        {
            byte[] byteBuffer = new byte[10];
            char[] charBuffer = new char[10];

            Assert.Throws<ObjectDisposedException>(() => binaryReader.PeekChar());
            Assert.Throws<ObjectDisposedException>(() => binaryReader.Read());
            Assert.Throws<ObjectDisposedException>(() => binaryReader.Read(byteBuffer, 0, 1));
            Assert.Throws<ObjectDisposedException>(() => binaryReader.Read(charBuffer, 0, 1));
            Assert.Throws<ObjectDisposedException>(() => binaryReader.ReadBoolean());
            Assert.Throws<ObjectDisposedException>(() => binaryReader.ReadByte());
            Assert.Throws<ObjectDisposedException>(() => binaryReader.ReadBytes(1));
            Assert.Throws<ObjectDisposedException>(() => binaryReader.ReadChar());
            Assert.Throws<ObjectDisposedException>(() => binaryReader.ReadChars(1));
            Assert.Throws<ObjectDisposedException>(() => binaryReader.ReadDecimal());
            Assert.Throws<ObjectDisposedException>(() => binaryReader.ReadDouble());
            Assert.Throws<ObjectDisposedException>(() => binaryReader.ReadInt16());
            Assert.Throws<ObjectDisposedException>(() => binaryReader.ReadInt32());
            Assert.Throws<ObjectDisposedException>(() => binaryReader.ReadInt64());
            Assert.Throws<ObjectDisposedException>(() => binaryReader.ReadSByte());
            Assert.Throws<ObjectDisposedException>(() => binaryReader.ReadSingle());
            Assert.Throws<ObjectDisposedException>(() => binaryReader.ReadString());
            Assert.Throws<ObjectDisposedException>(() => binaryReader.ReadUInt16());
            Assert.Throws<ObjectDisposedException>(() => binaryReader.ReadUInt32());
            Assert.Throws<ObjectDisposedException>(() => binaryReader.ReadUInt64());
        }

        public class NegEncoding : UTF8Encoding
        {
            public override Decoder GetDecoder()
            {
                return new NegDecoder();
            }

            public class NegDecoder : Decoder
            {
                public override int GetCharCount(byte[] bytes, int index, int count)
                {
                    return 1;
                }

                public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
                {
                    return -10000000;
                }
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Difference in behavior that added extra checks to BinaryReader/Writer buffers on .Net Core")]
        public void Read_InvalidEncoding()
        {
            using (var str = CreateStream())
            {
                byte[] memb = new byte[100];
                new Random(345).NextBytes(memb);
                str.Write(memb, 0, 100);
                str.Position = 0;

                using (var reader = new BinaryReader(str, new NegEncoding()))
                {
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("charsRemaining", () => reader.Read(new char[10], 0, 10));
                }
            }
        }

        [Theory]
        [InlineData(100, 0, 100, 100, 100)]
        [InlineData(100, 25, 50, 100, 50)]
        [InlineData(50, 0, 100, 100, 50)]
        [InlineData(0, 0, 10, 10, 0)]
        public void Read_CharArray(int sourceSize, int index, int count, int destinationSize, int expectedReadLength)
        {
            using (var stream = CreateStream())
            {
                var source = new char[sourceSize];
                var random = new Random(345);

                for (int i = 0; i < sourceSize; i++)
                {
                    source[i] = (char)random.Next(0, 127);
                }

                stream.Write(Encoding.ASCII.GetBytes(source), 0, source.Length);
                stream.Position = 0;

                using (var reader = new BinaryReader(stream, Encoding.ASCII))
                {
                    var destination = new char[destinationSize];

                    int readCount = reader.Read(destination, index, count);

                    Assert.Equal(expectedReadLength, readCount);
                    Assert.Equal(source.Take(readCount), destination.Skip(index).Take(readCount));

                    // Make sure we didn't write past the end
                    Assert.True(destination.Skip(readCount + index).All(b => b == default(char)));
                }
            }
        }

        [Theory]
        [InlineData(new[] { 'h', 'e', 'l', 'l', 'o' }, 5, new[] { 'h', 'e', 'l', 'l', 'o' })]
        [InlineData(new[] { 'h', 'e', 'l', 'l', 'o' }, 8, new[] { 'h', 'e', 'l', 'l', 'o' })]
        [InlineData(new[] { 'h', 'e', '\0', '\0', 'o' }, 5, new[] { 'h', 'e', '\0', '\0', 'o' })]
        [InlineData(new[] { 'h', 'e', 'l', 'l', 'o' }, 0, new char[0])]
        [InlineData(new char[0], 5, new char[0])]
        public void ReadChars(char[] source, int readLength, char[] expected)
        {
            using (var stream = CreateStream())
            {
                stream.Write(Encoding.ASCII.GetBytes(source), 0, source.Length);
                stream.Position = 0;

                using (var reader = new BinaryReader(stream))
                {
                    var destination = reader.ReadChars(readLength);

                    Assert.Equal(expected, destination);
                }
            }
        }
    }
}
