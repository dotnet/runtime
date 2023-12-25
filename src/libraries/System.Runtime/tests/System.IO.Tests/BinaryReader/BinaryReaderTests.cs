// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public class BinaryReaderTests
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

        [Fact]
        public void BinaryReader_EofReachedEarlyTests_ThrowsException()
        {
            // test integer primitives

            RunTest(writer => writer.Write(byte.MinValue), reader => reader.ReadByte());
            RunTest(writer => writer.Write(byte.MaxValue), reader => reader.ReadByte());
            RunTest(writer => writer.Write(sbyte.MinValue), reader => reader.ReadSByte());
            RunTest(writer => writer.Write(sbyte.MaxValue), reader => reader.ReadSByte());
            RunTest(writer => writer.Write(short.MinValue), reader => reader.ReadInt16());
            RunTest(writer => writer.Write(short.MaxValue), reader => reader.ReadInt16());
            RunTest(writer => writer.Write(ushort.MinValue), reader => reader.ReadUInt16());
            RunTest(writer => writer.Write(ushort.MaxValue), reader => reader.ReadUInt16());
            RunTest(writer => writer.Write(int.MinValue), reader => reader.ReadInt32());
            RunTest(writer => writer.Write(int.MaxValue), reader => reader.ReadInt32());
            RunTest(writer => writer.Write(uint.MinValue), reader => reader.ReadUInt32());
            RunTest(writer => writer.Write(uint.MaxValue), reader => reader.ReadUInt32());
            RunTest(writer => writer.Write(long.MinValue), reader => reader.ReadInt64());
            RunTest(writer => writer.Write(long.MaxValue), reader => reader.ReadInt64());
            RunTest(writer => writer.Write(ulong.MinValue), reader => reader.ReadUInt64());
            RunTest(writer => writer.Write(ulong.MaxValue), reader => reader.ReadUInt64());
            RunTest(writer => writer.Write7BitEncodedInt(int.MinValue), reader => reader.Read7BitEncodedInt());
            RunTest(writer => writer.Write7BitEncodedInt(int.MaxValue), reader => reader.Read7BitEncodedInt());
            RunTest(writer => writer.Write7BitEncodedInt64(long.MinValue), reader => reader.Read7BitEncodedInt64());
            RunTest(writer => writer.Write7BitEncodedInt64(long.MaxValue), reader => reader.Read7BitEncodedInt64());

            // test non-integer numeric types

            RunTest(writer => writer.Write((Half)0.1234), reader => reader.ReadHalf());
            RunTest(writer => writer.Write((float)0.1234), reader => reader.ReadSingle());
            RunTest(writer => writer.Write((double)0.1234), reader => reader.ReadDouble());
            RunTest(writer => writer.Write((decimal)0.1234), reader => reader.ReadDecimal());

            // test non-numeric primitive types

            RunTest(writer => writer.Write(true), reader => reader.ReadBoolean());
            RunTest(writer => writer.Write(false), reader => reader.ReadBoolean());
            RunTest(writer => writer.Write(string.Empty), reader => reader.ReadString());
            RunTest(writer => writer.Write("hello world"), reader => reader.ReadString());
            RunTest(writer => writer.Write(new string('x', 1024 * 1024)), reader => reader.ReadString());

            void RunTest(Action<BinaryWriter> writeAction, Action<BinaryReader> readAction)
            {
                UTF8Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
                MemoryStream memoryStream = new MemoryStream();

                // First, call the write action twice

                BinaryWriter writer = new BinaryWriter(memoryStream, encoding, leaveOpen: true);
                writeAction(writer);
                writeAction(writer);
                writer.Close();

                // Make sure we populated the inner stream, then truncate it before EOF reached.

                Assert.True(memoryStream.Length > 0);
                memoryStream.Position = 0; // reset read pointer
                memoryStream.SetLength(memoryStream.Length - 1); // truncate the last byte of the stream

                BinaryReader reader = new BinaryReader(memoryStream, encoding);
                readAction(reader); // should succeed
                Assert.Throws<EndOfStreamException>(() => readAction(reader)); // should fail
            }
        }

        /*
         * Other tests for Read7BitEncodedInt[64] are in BinaryWriter.WriteTests.cs, not here.
         */

        [Fact]
        public void BinaryReader_Read7BitEncodedInt_AllowsOverlongEncodings()
        {
            MemoryStream memoryStream = new MemoryStream(new byte[] { 0x9F, 0x00 /* overlong */ });
            BinaryReader reader = new BinaryReader(memoryStream);

            int actual = reader.Read7BitEncodedInt();
            Assert.Equal(0x1F, actual);
        }

        [Fact]
        public void BinaryReader_Read7BitEncodedInt_BadFormat_Throws()
        {
            // Serialized form of 0b1_00000000_00000000_00000000_00000000
            //                      |0x10|| 0x80 || 0x80 || 0x80 || 0x80|

            MemoryStream memoryStream = new MemoryStream(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x10 });
            BinaryReader reader = new BinaryReader(memoryStream);
            Assert.Throws<FormatException>(() => reader.Read7BitEncodedInt());

            // 5 bytes, all with the "there's more data after this" flag set

            memoryStream = new MemoryStream(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80 });
            reader = new BinaryReader(memoryStream);
            Assert.Throws<FormatException>(() => reader.Read7BitEncodedInt());
        }

        [Fact]
        public void BinaryReader_Read7BitEncodedInt64_AllowsOverlongEncodings()
        {
            MemoryStream memoryStream = new MemoryStream(new byte[] { 0x9F, 0x00 /* overlong */ });
            BinaryReader reader = new BinaryReader(memoryStream);

            long actual = reader.Read7BitEncodedInt64();
            Assert.Equal(0x1F, actual);
        }

        [Fact]
        public void BinaryReader_Read7BitEncodedInt64_BadFormat_Throws()
        {
            // Serialized form of 0b1_00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000
            //                      | || 0x80| | 0x80|| 0x80 || 0x80 || 0x80 || 0x80 || 0x80 || 0x80 || 0x80|
            //                       `-- 0x02

            MemoryStream memoryStream = new MemoryStream(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x02 });
            BinaryReader reader = new BinaryReader(memoryStream);
            Assert.Throws<FormatException>(() => reader.Read7BitEncodedInt64());

            // 10 bytes, all with the "there's more data after this" flag set

            memoryStream = new MemoryStream(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80 });
            reader = new BinaryReader(memoryStream);
            Assert.Throws<FormatException>(() => reader.Read7BitEncodedInt());
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
            Assert.Throws<ObjectDisposedException>(() => binaryReader.ReadHalf());
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
                    Assert.ThrowsAny<ArgumentException>(() => reader.Read(new char[10], 0, 10));
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

        // ChunkingStream returns less than requested
        private sealed class ChunkingStream : MemoryStream
        {
            public override int Read(byte[] buffer, int offset, int count)
            {
                return base.Read(buffer, offset, count > 10 ? count - 3 : count);
            }

            public override int Read(Span<byte> destination)
            {
                return base.Read(destination.Length > 10 ? destination.Slice(0, destination.Length - 3) : destination);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ReadChars_OverReads(bool unicodeEncoding)
        {
            Encoding encoding = unicodeEncoding ? Encoding.Unicode : Encoding.UTF8;

            char[] data1 = "hello world \ud83d\ude03!".ToCharArray(); // 14 code points, 15 chars in UTF-16, 17 bytes in UTF-8
            uint data2 = 0xABCDEF01;

            using (Stream stream = new ChunkingStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, encoding, leaveOpen: true))
                {
                    writer.Write(data1);
                    writer.Write(data2);
                }

                stream.Seek(0, SeekOrigin.Begin);
                using (BinaryReader reader = new BinaryReader(stream, encoding, leaveOpen: true))
                {
                    Assert.Equal(data1, reader.ReadChars(data1.Length));
                    Assert.Equal(data2, reader.ReadUInt32());
                }
            }
        }

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

        private class DerivedBinaryReader : BinaryReader
        {
            public DerivedBinaryReader(Stream input) : base(input) { }

            public void CallFillBuffer0()
            {
                FillBuffer(0);
            }
        }

        [Fact]
        public void FillBuffer_Zero_Throws()
        {
            using Stream stream = CreateStream();

            string hello = "Hello";
            stream.Write(Encoding.ASCII.GetBytes(hello));
            stream.Position = 0;

            using var derivedReader = new DerivedBinaryReader(stream);
            Assert.Throws<EndOfStreamException>(derivedReader.CallFillBuffer0);
        }
    }
}
