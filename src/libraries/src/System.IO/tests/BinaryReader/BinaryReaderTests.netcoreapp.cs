// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public partial class BinaryReaderTests
    {
        [Theory]
        [InlineData(100, 100, 100)]
        [InlineData(100, 50, 50)]
        [InlineData(50, 100, 50)]
        [InlineData(10, 0, 0)]
        [InlineData(0, 10, 0)]
        public void Read_ByteSpan(int sourceSize, int destinationSize, int expectedReadLength)
        {
            using (var stream = CreateStream())
            {
                var source = new byte[sourceSize];
                new Random(345).NextBytes(source);
                stream.Write(source, 0, source.Length);
                stream.Position = 0;

                using (var reader = new BinaryReader(stream))
                {
                    var destination = new byte[destinationSize];

                    int readCount = reader.Read(new Span<byte>(destination));

                    Assert.Equal(expectedReadLength, readCount);
                    Assert.Equal(source.Take(expectedReadLength), destination.Take(expectedReadLength));

                    // Make sure we didn't write past the end
                    Assert.True(destination.Skip(expectedReadLength).All(b => b == default(byte)));
                }
            }
        }

        [Fact]
        public void Read_ByteSpan_ThrowIfDisposed()
        {
            using (var memStream = CreateStream())
            {
                var binaryReader = new BinaryReader(memStream);
                binaryReader.Dispose();
                Assert.Throws<ObjectDisposedException>(() => binaryReader.Read(new Span<byte>()));
            }
        }

        [Theory]
        [InlineData(100, 100, 100)]
        [InlineData(100, 50, 50)]
        [InlineData(50, 100, 50)]
        [InlineData(10, 0, 0)]
        [InlineData(0, 10, 0)]
        public void Read_CharSpan(int sourceSize, int destinationSize, int expectedReadLength)
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

                    int readCount = reader.Read(new Span<char>(destination));

                    Assert.Equal(expectedReadLength, readCount);
                    Assert.Equal(source.Take(expectedReadLength), destination.Take(expectedReadLength));

                    // Make sure we didn't write past the end
                    Assert.True(destination.Skip(expectedReadLength).All(b => b == default(char)));
                }
            }
        }

        [Fact]
        public void Read_CharSpan_ThrowIfDisposed()
        {
            using (var memStream = CreateStream())
            {
                var binaryReader = new BinaryReader(memStream);
                binaryReader.Dispose();
                Assert.Throws<ObjectDisposedException>(() => binaryReader.Read(new Span<char>()));
            }
        }
    }
}
