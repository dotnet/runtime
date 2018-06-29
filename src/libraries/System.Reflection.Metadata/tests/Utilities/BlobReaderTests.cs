// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Internal;
using System.Text;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class BlobReaderTests
    {
        [Fact]
        public unsafe void Properties()
        {
            byte[] buffer = new byte[] { 0, 1, 0, 2, 5, 6 };

            fixed (byte* bufferPtr = buffer)
            {
                var reader = new BlobReader(bufferPtr, 4);
                Assert.True(reader.StartPointer == bufferPtr);
                Assert.True(reader.CurrentPointer == bufferPtr);
                Assert.Equal(0, reader.Offset);
                Assert.Equal(4, reader.RemainingBytes);
                Assert.Equal(4, reader.Length);

                Assert.Equal(0, reader.ReadByte());
                Assert.True(reader.StartPointer == bufferPtr);
                Assert.True(reader.CurrentPointer == bufferPtr + 1);
                Assert.Equal(1, reader.Offset);
                Assert.Equal(3, reader.RemainingBytes);
                Assert.Equal(4, reader.Length);

                Assert.Equal(1, reader.ReadInt16());
                Assert.True(reader.StartPointer == bufferPtr);
                Assert.True(reader.CurrentPointer == bufferPtr + 3);
                Assert.Equal(3, reader.Offset);
                Assert.Equal(1, reader.RemainingBytes);
                Assert.Equal(4, reader.Length);

                Assert.Throws<BadImageFormatException>(() => reader.ReadInt16());
                Assert.True(reader.StartPointer == bufferPtr);
                Assert.True(reader.CurrentPointer == bufferPtr + 3);
                Assert.Equal(3, reader.Offset);
                Assert.Equal(1, reader.RemainingBytes);
                Assert.Equal(4, reader.Length);

                Assert.Equal(2, reader.ReadByte());
                Assert.True(reader.StartPointer == bufferPtr);
                Assert.True(reader.CurrentPointer == bufferPtr + 4);
                Assert.Equal(4, reader.Offset);
                Assert.Equal(0, reader.RemainingBytes);
                Assert.Equal(4, reader.Length);
            }
        }

        [Fact]
        public unsafe void Offset()
        {
            byte[] buffer = new byte[] { 0, 1, 0, 2, 5, 6 };

            fixed (byte* bufferPtr = buffer)
            {
                var reader = new BlobReader(bufferPtr, 4);
                Assert.Equal(0, reader.Offset);

                reader.Offset = 0;
                Assert.Equal(0, reader.Offset);
                Assert.Equal(4, reader.RemainingBytes);
                Assert.True(reader.CurrentPointer == bufferPtr);

                reader.Offset = 3;
                Assert.Equal(3, reader.Offset);
                Assert.Equal(1, reader.RemainingBytes);
                Assert.True(reader.CurrentPointer == bufferPtr + 3);

                reader.Offset = 1;
                Assert.Equal(1, reader.Offset);
                Assert.Equal(3, reader.RemainingBytes);
                Assert.True(reader.CurrentPointer == bufferPtr + 1);

                Assert.Equal(1, reader.ReadByte());
                Assert.Equal(2, reader.Offset);
                Assert.Equal(2, reader.RemainingBytes);
                Assert.True(reader.CurrentPointer == bufferPtr + 2);

                reader.Offset = 4;
                Assert.Equal(4, reader.Offset);
                Assert.Equal(0, reader.RemainingBytes);
                Assert.True(reader.CurrentPointer == bufferPtr + 4);

                Assert.Throws<BadImageFormatException>(() => reader.Offset = 5);
                Assert.Equal(4, reader.Offset);
                Assert.Equal(0, reader.RemainingBytes);
                Assert.True(reader.CurrentPointer == bufferPtr + 4);

                Assert.Throws<BadImageFormatException>(() => reader.Offset = -1);
                Assert.Equal(4, reader.Offset);
                Assert.Equal(0, reader.RemainingBytes);
                Assert.True(reader.CurrentPointer == bufferPtr + 4);

                Assert.Throws<BadImageFormatException>(() => reader.Offset = int.MaxValue);
                Assert.Throws<BadImageFormatException>(() => reader.Offset = int.MinValue);
            }
        }

        [Fact]
        public unsafe void PublicBlobReaderCtorValidatesArgs()
        {
            byte* bufferPtrForLambda;
            byte[] buffer = new byte[4] { 0, 1, 0, 2 };

            fixed (byte* bufferPtr = buffer)
            {
                bufferPtrForLambda = bufferPtr;
                Assert.Throws<ArgumentOutOfRangeException>(() => new BlobReader(bufferPtrForLambda, -1));
            }

            Assert.Throws<ArgumentNullException>(() => new BlobReader(null, 1));

            Assert.Equal(0, new BlobReader(null, 0).Length); // this is valid
            Assert.Throws<BadImageFormatException>(() => new BlobReader(null, 0).ReadByte()); // but can't read anything non-empty from it...
            Assert.Same(string.Empty, new BlobReader(null, 0).ReadUtf8NullTerminated()); // can read empty string.
        }

        [Fact]
        public unsafe void ReadBoolean1()
        {
            byte[] buffer = new byte[] { 1, 0xff, 0, 2 };
            fixed (byte* bufferPtr = buffer)
            {
                var reader = new BlobReader(new MemoryBlock(bufferPtr, buffer.Length));

                Assert.True(reader.ReadBoolean());
                Assert.True(reader.ReadBoolean());
                Assert.False(reader.ReadBoolean());
                Assert.True(reader.ReadBoolean());
            }
        }

        [Fact]
        public unsafe void ReadFromMemoryReader()
        {
            byte[] buffer = new byte[4] { 0, 1, 0, 2 };
            fixed (byte* bufferPtr = buffer)
            {
                var reader = new BlobReader(new MemoryBlock(bufferPtr, buffer.Length));

                Assert.Equal(0, reader.Offset);
                Assert.Throws<BadImageFormatException>(() => reader.ReadUInt64());
                Assert.Equal(0, reader.Offset);

                reader.Offset = 1;
                Assert.Throws<BadImageFormatException>(() => reader.ReadDouble());
                Assert.Equal(1, reader.Offset);

                reader.Offset = 2;
                Assert.Throws<BadImageFormatException>(() => reader.ReadUInt32());
                Assert.Equal((ushort)0x0200, reader.ReadUInt16());
                Assert.Equal(4, reader.Offset);

                reader.Offset = 2;
                Assert.Throws<BadImageFormatException>(() => reader.ReadSingle());
                Assert.Equal(2, reader.Offset);

                reader.Offset = 0;
                Assert.Equal(9.404242E-38F, reader.ReadSingle());
                Assert.Equal(4, reader.Offset);

                reader.Offset = 3;
                Assert.Throws<BadImageFormatException>(() => reader.ReadUInt16());
                Assert.Equal((byte)0x02, reader.ReadByte());
                Assert.Equal(4, reader.Offset);

                reader.Offset = 0;
                Assert.Equal("\u0000\u0001\u0000\u0002", reader.ReadUTF8(4));
                Assert.Equal(4, reader.Offset);

                reader.Offset = 0;
                Assert.Throws<BadImageFormatException>(() => reader.ReadUTF8(5));
                Assert.Equal(0, reader.Offset);

                reader.Offset = 0;
                Assert.Throws<BadImageFormatException>(() => reader.ReadUTF8(-1));
                Assert.Equal(0, reader.Offset);

                reader.Offset = 0;
                Assert.Equal("\u0100\u0200", reader.ReadUTF16(4));
                Assert.Equal(4, reader.Offset);

                reader.Offset = 0;
                Assert.Throws<BadImageFormatException>(() => reader.ReadUTF16(5));
                Assert.Equal(0, reader.Offset);

                reader.Offset = 0;
                Assert.Throws<BadImageFormatException>(() => reader.ReadUTF16(-1));
                Assert.Equal(0, reader.Offset);

                reader.Offset = 0;
                Assert.Throws<BadImageFormatException>(() => reader.ReadUTF16(6));
                Assert.Equal(0, reader.Offset);

                reader.Offset = 0;
                Assert.Equal(buffer, reader.ReadBytes(4));
                Assert.Equal(4, reader.Offset);

                reader.Offset = 0;
                Assert.Same(string.Empty, reader.ReadUtf8NullTerminated());
                Assert.Equal(1, reader.Offset);

                reader.Offset = 1;
                Assert.Equal("\u0001", reader.ReadUtf8NullTerminated());
                Assert.Equal(3, reader.Offset);

                reader.Offset = 3;
                Assert.Equal("\u0002", reader.ReadUtf8NullTerminated());
                Assert.Equal(4, reader.Offset);

                reader.Offset = 0;
                Assert.Same(string.Empty, reader.ReadUtf8NullTerminated());
                Assert.Equal(1, reader.Offset);

                reader.Offset = 0;
                Assert.Throws<BadImageFormatException>(() => reader.ReadBytes(5));
                Assert.Equal(0, reader.Offset);

                reader.Offset = 0;
                Assert.Throws<BadImageFormatException>(() => reader.ReadBytes(int.MinValue));
                Assert.Equal(0, reader.Offset);

                reader.Offset = 0;
                Assert.Throws<BadImageFormatException>(() => reader.GetMemoryBlockAt(-1, 1));
                Assert.Equal(0, reader.Offset);

                reader.Offset = 0;
                Assert.Throws<BadImageFormatException>(() => reader.GetMemoryBlockAt(1, -1));
                Assert.Equal(0, reader.Offset);

                reader.Offset = 0;
                Assert.Equal(3, reader.GetMemoryBlockAt(1, 3).Length);
                Assert.Equal(0, reader.Offset);

                reader.Offset = 3;
                reader.ReadByte();
                Assert.Equal(4, reader.Offset);

                reader.Offset = 4;
                Assert.Equal(0, reader.ReadBytes(0).Length);

                reader.Offset = 4;
                int value;
                Assert.False(reader.TryReadCompressedInteger(out value));
                Assert.Equal(BlobReader.InvalidCompressedInteger, value);

                reader.Offset = 4;
                Assert.Throws<BadImageFormatException>(() => reader.ReadCompressedInteger());

                reader.Offset = 4;
                Assert.Equal(SerializationTypeCode.Invalid, reader.ReadSerializationTypeCode());

                reader.Offset = 4;
                Assert.Equal(SignatureTypeCode.Invalid, reader.ReadSignatureTypeCode());

                reader.Offset = 4;
                Assert.Equal(default(EntityHandle), reader.ReadTypeHandle());

                reader.Offset = 4;
                Assert.Throws<BadImageFormatException>(() => reader.ReadBoolean());

                reader.Offset = 4;
                Assert.Throws<BadImageFormatException>(() => reader.ReadByte());

                reader.Offset = 4;
                Assert.Throws<BadImageFormatException>(() => reader.ReadSByte());

                reader.Offset = 4;
                Assert.Throws<BadImageFormatException>(() => reader.ReadUInt32());

                reader.Offset = 4;
                Assert.Throws<BadImageFormatException>(() => reader.ReadInt32());

                reader.Offset = 4;
                Assert.Throws<BadImageFormatException>(() => reader.ReadUInt64());

                reader.Offset = 4;
                Assert.Throws<BadImageFormatException>(() => reader.ReadInt64());

                reader.Offset = 4;
                Assert.Throws<BadImageFormatException>(() => reader.ReadSingle());

                reader.Offset = 4;
                Assert.Throws<BadImageFormatException>(() => reader.ReadDouble());

                reader.Offset = 4;
            }

            byte[] buffer2 = new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 };
            fixed (byte* bufferPtr2 = buffer2)
            {
                var reader = new BlobReader(new MemoryBlock(bufferPtr2, buffer2.Length));
                Assert.Equal(reader.Offset, 0);
                Assert.Equal(0x0807060504030201UL, reader.ReadUInt64());
                Assert.Equal(reader.Offset, 8);

                reader.Reset();
                Assert.Equal(reader.Offset, 0);
                Assert.Equal(0x0807060504030201L, reader.ReadInt64());

                reader.Reset();
                Assert.Equal(reader.Offset, 0);
                Assert.Equal(BitConverter.ToDouble(buffer2, 0), reader.ReadDouble());
            }
        }

        [Fact]
        public unsafe void ValidatePeekReferenceSize()
        {
            byte[] buffer = new byte[8] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 };
            fixed (byte* bufferPtr = buffer)
            {
                var block = new MemoryBlock(bufferPtr, buffer.Length);

                // small ref size always fits in 16 bits
                Assert.Equal(0xFFFF, block.PeekReference(0, smallRefSize: true));
                Assert.Equal(0xFFFF, block.PeekReference(4, smallRefSize: true));
                Assert.Equal(0xFFFFU, block.PeekTaggedReference(0, smallRefSize: true));
                Assert.Equal(0xFFFFU, block.PeekTaggedReference(4, smallRefSize: true));
                Assert.Equal(0x01FFU, block.PeekTaggedReference(6, smallRefSize: true));

                // large ref size throws on > RIDMask when tagged variant is not used.
                Assert.Throws<BadImageFormatException>(() => block.PeekReference(0, smallRefSize: false));
                Assert.Throws<BadImageFormatException>(() => block.PeekReference(4, smallRefSize: false));

                // large ref size does not throw when Tagged variant is used.
                Assert.Equal(0xFFFFFFFFU, block.PeekTaggedReference(0, smallRefSize: false));
                Assert.Equal(0x01FFFFFFU, block.PeekTaggedReference(4, smallRefSize: false));

                // bounds check applies in all cases
                Assert.Throws<BadImageFormatException>(() => block.PeekReference(7, smallRefSize: true));
                Assert.Throws<BadImageFormatException>(() => block.PeekReference(5, smallRefSize: false));
            }
        }

        [Fact]
        public unsafe void ReadFromMemoryBlock()
        {
            byte[] buffer = new byte[4] { 0, 1, 0, 2 };
            fixed (byte* bufferPtr = buffer)
            {
                var block = new MemoryBlock(bufferPtr, buffer.Length);

                Assert.Throws<BadImageFormatException>(() => block.PeekUInt32(int.MaxValue));
                Assert.Throws<BadImageFormatException>(() => block.PeekUInt32(-1));
                Assert.Throws<BadImageFormatException>(() => block.PeekUInt32(int.MinValue));
                Assert.Throws<BadImageFormatException>(() => block.PeekUInt32(4));
                Assert.Throws<BadImageFormatException>(() => block.PeekUInt32(1));
                Assert.Equal(0x02000100U, block.PeekUInt32(0));

                Assert.Throws<BadImageFormatException>(() => block.PeekUInt16(int.MaxValue));
                Assert.Throws<BadImageFormatException>(() => block.PeekUInt16(-1));
                Assert.Throws<BadImageFormatException>(() => block.PeekUInt16(int.MinValue));
                Assert.Throws<BadImageFormatException>(() => block.PeekUInt16(4));
                Assert.Equal(0x0200, block.PeekUInt16(2));

                int bytesRead;

                MetadataStringDecoder stringDecoder = MetadataStringDecoder.DefaultUTF8;
                Assert.Throws<BadImageFormatException>(() => block.PeekUtf8NullTerminated(int.MaxValue, null, stringDecoder, out bytesRead));
                Assert.Throws<BadImageFormatException>(() => block.PeekUtf8NullTerminated(-1, null, stringDecoder, out bytesRead));
                Assert.Throws<BadImageFormatException>(() => block.PeekUtf8NullTerminated(int.MinValue, null, stringDecoder, out bytesRead));
                Assert.Throws<BadImageFormatException>(() => block.PeekUtf8NullTerminated(5, null, stringDecoder, out bytesRead));

                Assert.Throws<BadImageFormatException>(() => block.GetMemoryBlockAt(-1, 1));
                Assert.Throws<BadImageFormatException>(() => block.GetMemoryBlockAt(1, -1));
                Assert.Throws<BadImageFormatException>(() => block.GetMemoryBlockAt(0, -1));
                Assert.Throws<BadImageFormatException>(() => block.GetMemoryBlockAt(-1, 0));
                Assert.Throws<BadImageFormatException>(() => block.GetMemoryBlockAt(-int.MaxValue, int.MaxValue));
                Assert.Throws<BadImageFormatException>(() => block.GetMemoryBlockAt(int.MaxValue, -int.MaxValue));
                Assert.Throws<BadImageFormatException>(() => block.GetMemoryBlockAt(int.MaxValue, int.MaxValue));
                Assert.Throws<BadImageFormatException>(() => block.GetMemoryBlockAt(block.Length, -1));
                Assert.Throws<BadImageFormatException>(() => block.GetMemoryBlockAt(-1, block.Length));


                Assert.Equal("\u0001", block.PeekUtf8NullTerminated(1, null, stringDecoder, out bytesRead));
                Assert.Equal(bytesRead, 2);

                Assert.Equal("\u0002", block.PeekUtf8NullTerminated(3, null, stringDecoder, out bytesRead));
                Assert.Equal(bytesRead, 1);

                Assert.Equal("", block.PeekUtf8NullTerminated(4, null, stringDecoder, out bytesRead));
                Assert.Equal(bytesRead, 0);

                byte[] helloPrefix = Encoding.UTF8.GetBytes("Hello");

                Assert.Equal("Hello\u0001", block.PeekUtf8NullTerminated(1, helloPrefix, stringDecoder, out bytesRead));
                Assert.Equal(bytesRead, 2);

                Assert.Equal("Hello\u0002", block.PeekUtf8NullTerminated(3, helloPrefix, stringDecoder, out bytesRead));
                Assert.Equal(bytesRead, 1);

                Assert.Equal("Hello", block.PeekUtf8NullTerminated(4, helloPrefix, stringDecoder, out bytesRead));
                Assert.Equal(bytesRead, 0);
            }
        }

        [Fact]
        public unsafe void IndexOf()
        {
            byte[] buffer = new byte[]
            {
                0xF0, 0x90, 0x8D,
            };

            fixed (byte* bufferPtr = buffer)
            {
                var reader = new BlobReader(bufferPtr, buffer.Length);

                Assert.Equal(0, reader.IndexOf(0xF0));
                Assert.Equal(1, reader.IndexOf(0x90));
                Assert.Equal(2, reader.IndexOf(0x8D));
                Assert.Equal(-1, reader.IndexOf(0x8C));
                Assert.Equal(-1, reader.IndexOf(0));
                Assert.Equal(-1, reader.IndexOf(0xff));

                reader.ReadByte();
                Assert.Equal(-1, reader.IndexOf(0xF0));
                Assert.Equal(0, reader.IndexOf(0x90));
                Assert.Equal(1, reader.IndexOf(0x8D));

                reader.ReadByte();
                Assert.Equal(-1, reader.IndexOf(0xF0));
                Assert.Equal(-1, reader.IndexOf(0x90));
                Assert.Equal(0, reader.IndexOf(0x8D));

                reader.ReadByte();
                Assert.Equal(-1, reader.IndexOf(0xF0));
                Assert.Equal(-1, reader.IndexOf(0x90));
                Assert.Equal(-1, reader.IndexOf(0x8D));
            }
        }
    }
}
