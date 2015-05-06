// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.Internal;
using System.Text;
using TestUtilities;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class BlobReaderTests
    {
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
            Assert.Same(String.Empty, new BlobReader(null, 0).ReadUtf8NullTerminated()); // can read empty string.
        }

        [Fact]
        public unsafe void ReadFromMemoryReader()
        {
            byte[] buffer = new byte[4] { 0, 1, 0, 2 };
            fixed (byte* bufferPtr = buffer)
            {
                var reader = new BlobReader(new MemoryBlock(bufferPtr, buffer.Length));

                Assert.False(reader.SeekOffset(-1));
                Assert.False(reader.SeekOffset(Int32.MaxValue));
                Assert.False(reader.SeekOffset(Int32.MinValue));
                Assert.False(reader.SeekOffset(buffer.Length));
                Assert.True(reader.SeekOffset(buffer.Length - 1));

                Assert.True(reader.SeekOffset(0));
                Assert.Equal(0, reader.Offset);
                Assert.Throws<BadImageFormatException>(() => reader.ReadUInt64());
                Assert.Equal(0, reader.Offset);

                Assert.True(reader.SeekOffset(1));
                Assert.Equal(1, reader.Offset);
                Assert.Throws<BadImageFormatException>(() => reader.ReadDouble());
                Assert.Equal(1, reader.Offset);

                Assert.True(reader.SeekOffset(2));
                Assert.Equal(2, reader.Offset);
                Assert.Throws<BadImageFormatException>(() => reader.ReadUInt32());
                Assert.Equal((ushort)0x0200, reader.ReadUInt16());
                Assert.Equal(4, reader.Offset);

                Assert.True(reader.SeekOffset(2));
                Assert.Equal(2, reader.Offset);
                Assert.Throws<BadImageFormatException>(() => reader.ReadSingle());
                Assert.Equal(2, reader.Offset);

                Assert.True(reader.SeekOffset(0));
                Assert.Equal(9.404242E-38F, reader.ReadSingle());
                Assert.Equal(4, reader.Offset);

                Assert.True(reader.SeekOffset(3));
                Assert.Equal(3, reader.Offset);
                Assert.Throws<BadImageFormatException>(() => reader.ReadUInt16());
                Assert.Equal((byte)0x02, reader.ReadByte());
                Assert.Equal(4, reader.Offset);

                Assert.True(reader.SeekOffset(0));
                Assert.Equal("\u0000\u0001\u0000\u0002", reader.ReadUTF8(4));
                Assert.Equal(4, reader.Offset);

                Assert.True(reader.SeekOffset(0));
                Assert.Throws<BadImageFormatException>(() => reader.ReadUTF8(5));
                Assert.Equal(0, reader.Offset);

                Assert.True(reader.SeekOffset(0));
                Assert.Throws<BadImageFormatException>(() => reader.ReadUTF8(-1));
                Assert.Equal(0, reader.Offset);

                Assert.True(reader.SeekOffset(0));
                Assert.Equal("\u0100\u0200", reader.ReadUTF16(4));
                Assert.Equal(4, reader.Offset);

                Assert.True(reader.SeekOffset(0));
                Assert.Throws<BadImageFormatException>(() => reader.ReadUTF16(5));
                Assert.Equal(0, reader.Offset);

                Assert.True(reader.SeekOffset(0));
                Assert.Throws<BadImageFormatException>(() => reader.ReadUTF16(-1));
                Assert.Equal(0, reader.Offset);

                Assert.True(reader.SeekOffset(0));
                Assert.Throws<BadImageFormatException>(() => reader.ReadUTF16(6));
                Assert.Equal(0, reader.Offset);

                Assert.True(reader.SeekOffset(0));
                AssertEx.Equal(buffer, reader.ReadBytes(4));
                Assert.Equal(4, reader.Offset);

                Assert.True(reader.SeekOffset(0));
                Assert.Same(String.Empty, reader.ReadUtf8NullTerminated());
                Assert.Equal(1, reader.Offset);

                Assert.True(reader.SeekOffset(1));
                Assert.Equal("\u0001", reader.ReadUtf8NullTerminated());
                Assert.Equal(3, reader.Offset);

                Assert.True(reader.SeekOffset(3));
                Assert.Equal("\u0002", reader.ReadUtf8NullTerminated());
                Assert.Equal(4, reader.Offset);

                Assert.True(reader.SeekOffset(0));
                Assert.Same(String.Empty, reader.ReadUtf8NullTerminated());
                Assert.Equal(1, reader.Offset);

                Assert.True(reader.SeekOffset(0));
                Assert.Throws<BadImageFormatException>(() => reader.ReadBytes(5));
                Assert.Equal(0, reader.Offset);

                Assert.True(reader.SeekOffset(0));
                Assert.Throws<BadImageFormatException>(() => reader.ReadBytes(Int32.MinValue));
                Assert.Equal(0, reader.Offset);

                Assert.True(reader.SeekOffset(0));
                Assert.Throws<BadImageFormatException>(() => reader.GetMemoryBlockAt(-1, 1));
                Assert.Equal(0, reader.Offset);

                Assert.True(reader.SeekOffset(0));
                Assert.Throws<BadImageFormatException>(() => reader.GetMemoryBlockAt(1, -1));
                Assert.Equal(0, reader.Offset);

                Assert.True(reader.SeekOffset(0));
                Assert.Equal(3, reader.GetMemoryBlockAt(1, 3).Length);
                Assert.Equal(0, reader.Offset);

                Assert.True(reader.SeekOffset(3));
                reader.ReadByte();
                Assert.Equal(4, reader.Offset);

                Assert.Equal(4, reader.Offset);
                Assert.Equal(0, reader.ReadBytes(0).Length);

                Assert.Equal(4, reader.Offset);
                int value;
                Assert.False(reader.TryReadCompressedInteger(out value));
                Assert.Equal(BlobReader.InvalidCompressedInteger, value);

                Assert.Equal(4, reader.Offset);
                Assert.Throws<BadImageFormatException>(() => reader.ReadCompressedInteger());

                Assert.Equal(4, reader.Offset);
                Assert.Equal(SerializationTypeCode.Invalid, reader.ReadSerializationTypeCode());

                Assert.Equal(4, reader.Offset);
                Assert.Equal(SignatureTypeCode.Invalid, reader.ReadSignatureTypeCode());

                Assert.Equal(4, reader.Offset);
                Assert.Equal(default(EntityHandle), reader.ReadTypeHandle());

                Assert.Equal(4, reader.Offset);
                Assert.Throws<BadImageFormatException>(() => reader.ReadBoolean());

                Assert.Equal(4, reader.Offset);
                Assert.Throws<BadImageFormatException>(() => reader.ReadByte());

                Assert.Equal(4, reader.Offset);
                Assert.Throws<BadImageFormatException>(() => reader.ReadSByte());

                Assert.Equal(4, reader.Offset);
                Assert.Throws<BadImageFormatException>(() => reader.ReadUInt32());

                Assert.Equal(4, reader.Offset);
                Assert.Throws<BadImageFormatException>(() => reader.ReadInt32());

                Assert.Equal(4, reader.Offset);
                Assert.Throws<BadImageFormatException>(() => reader.ReadUInt64());

                Assert.Equal(4, reader.Offset);
                Assert.Throws<BadImageFormatException>(() => reader.ReadInt64());

                Assert.Equal(4, reader.Offset);
                Assert.Throws<BadImageFormatException>(() => reader.ReadSingle());

                Assert.Equal(4, reader.Offset);
                Assert.Throws<BadImageFormatException>(() => reader.ReadDouble());

                Assert.Equal(4, reader.Offset);
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
                AssertEx.Throws<BadImageFormatException>(() => block.PeekReference(0, smallRefSize: false), MetadataResources.RowIdOrHeapOffsetTooLarge);
                AssertEx.Throws<BadImageFormatException>(() => block.PeekReference(4, smallRefSize: false), MetadataResources.RowIdOrHeapOffsetTooLarge);

                // large ref size does not throw when Tagged variant is used.
                Assert.Equal(0xFFFFFFFFU, block.PeekTaggedReference(0, smallRefSize: false));
                Assert.Equal(0x01FFFFFFU, block.PeekTaggedReference(4, smallRefSize: false));

                // bounds check applies in all cases
                AssertEx.Throws<BadImageFormatException>(() => block.PeekReference(7, smallRefSize: true), MetadataResources.OutOfBoundsRead);
                AssertEx.Throws<BadImageFormatException>(() => block.PeekReference(5, smallRefSize: false), MetadataResources.OutOfBoundsRead);
            }
        }

        [Fact]
        public unsafe void ReadFromMemoryBlock()
        {
            byte[] buffer = new byte[4] { 0, 1, 0, 2 };
            fixed (byte* bufferPtr = buffer)
            {
                var block = new MemoryBlock(bufferPtr, buffer.Length);

                Assert.Throws<BadImageFormatException>(() => block.PeekUInt32(Int32.MaxValue));
                Assert.Throws<BadImageFormatException>(() => block.PeekUInt32(-1));
                Assert.Throws<BadImageFormatException>(() => block.PeekUInt32(Int32.MinValue));
                Assert.Throws<BadImageFormatException>(() => block.PeekUInt32(4));
                Assert.Throws<BadImageFormatException>(() => block.PeekUInt32(1));
                Assert.Equal(0x02000100U, block.PeekUInt32(0));

                Assert.Throws<BadImageFormatException>(() => block.PeekUInt16(Int32.MaxValue));
                Assert.Throws<BadImageFormatException>(() => block.PeekUInt16(-1));
                Assert.Throws<BadImageFormatException>(() => block.PeekUInt16(Int32.MinValue));
                Assert.Throws<BadImageFormatException>(() => block.PeekUInt16(4));
                Assert.Equal(0x0200, block.PeekUInt16(2));

                int bytesRead;

                MetadataStringDecoder stringDecoder = MetadataStringDecoder.DefaultUTF8;
                Assert.Throws<BadImageFormatException>(() => block.PeekUtf8NullTerminated(Int32.MaxValue, null, stringDecoder, out bytesRead));
                Assert.Throws<BadImageFormatException>(() => block.PeekUtf8NullTerminated(-1, null, stringDecoder, out bytesRead));
                Assert.Throws<BadImageFormatException>(() => block.PeekUtf8NullTerminated(Int32.MinValue, null, stringDecoder, out bytesRead));
                Assert.Throws<BadImageFormatException>(() => block.PeekUtf8NullTerminated(5, null, stringDecoder, out bytesRead));

                Assert.Throws<BadImageFormatException>(() => block.GetMemoryBlockAt(-1, 1));
                Assert.Throws<BadImageFormatException>(() => block.GetMemoryBlockAt(1, -1));
                Assert.Throws<BadImageFormatException>(() => block.GetMemoryBlockAt(0, -1));
                Assert.Throws<BadImageFormatException>(() => block.GetMemoryBlockAt(-1, 0));
                Assert.Throws<BadImageFormatException>(() => block.GetMemoryBlockAt(-Int32.MaxValue, Int32.MaxValue));
                Assert.Throws<BadImageFormatException>(() => block.GetMemoryBlockAt(Int32.MaxValue, -Int32.MaxValue));
                Assert.Throws<BadImageFormatException>(() => block.GetMemoryBlockAt(Int32.MaxValue, Int32.MaxValue));
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
    }
}
