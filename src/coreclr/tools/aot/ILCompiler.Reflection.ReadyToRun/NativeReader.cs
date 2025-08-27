// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun
{
    public class NativeReader(Stream backingStream, bool littleEndian = true)
    {
        private const int BITS_PER_BYTE = 8;
        private const int BITS_PER_SIZE_T = 32;

        private readonly Stream _backingStream = backingStream;
        private readonly bool _littleEndian = littleEndian;

        /// <summary>
        /// Reads a byte from the image at the specified index
        /// </summary>
        public int this[int index]
        {
            get => ReadByte(ref index);
        }

        /// <summary>
        /// Reads a span of bytes from the image at the specified start index
        /// </summary>
        /// <param name="start">Starting index of the value</param>
        /// <param name="buffer">Span to be filled with the read bytes</param>
        /// <remarks>
        /// The <paramref name="start"/> gets incremented by the size of the buffer
        /// </remarks>
        public void ReadSpanAt(ref int start, Span<byte> buffer)
        {
            if (start < 0 || start + buffer.Length > _backingStream.Length)
                throw new ArgumentOutOfRangeException(nameof(start), "Start index is out of bounds");

            _backingStream.Seek(start, SeekOrigin.Begin);
            _backingStream.ReadExactly(buffer);
            start += buffer.Length;
        }

        /// <summary>
        /// Extracts a 64bit value from the image byte array
        /// </summary>
        /// <param name="start">Starting index of the value</param>
        /// <remarks>
        /// The <paramref name="start"/> gets incremented by the size of the value
        /// </remarks>
        public long ReadInt64(ref int start)
        {
            Span<byte> bytes = stackalloc byte[sizeof(long)];
            ReadSpanAt(ref start, bytes);
            return _littleEndian ? BinaryPrimitives.ReadInt64LittleEndian(bytes) : BinaryPrimitives.ReadInt64BigEndian(bytes);
        }

        // <summary>
        /// Extracts a 32bit value from the image byte array
        /// </summary>
        /// <param name="start">Starting index of the value</param>
        /// <remarks>
        /// The <paramref name="start"/> gets incremented by the size of the value
        /// </remarks>
        public int ReadInt32(ref int start)
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];
            ReadSpanAt(ref start, bytes);
            return _littleEndian ? BinaryPrimitives.ReadInt32LittleEndian(bytes) : BinaryPrimitives.ReadInt32BigEndian(bytes);
        }

        // <summary>
        /// Extracts an unsigned 32bit value from the image byte array
        /// </summary>
        /// <param name="start">Starting index of the value</param>
        /// <remarks>
        /// The <paramref name="start"/> gets incremented by the size of the value
        /// </remarks>
        public uint ReadUInt32(ref int start)
        {
            Span<byte> bytes = stackalloc byte[sizeof(uint)];
            ReadSpanAt(ref start, bytes);
            return _littleEndian ? BinaryPrimitives.ReadUInt32LittleEndian(bytes) : BinaryPrimitives.ReadUInt32BigEndian(bytes);
        }

        // <summary>
        /// Extracts an unsigned 16bit value from the image byte array
        /// </summary>
        /// <param name="start">Starting index of the value</param>
        /// <remarks>
        /// The <paramref name="start"/> gets incremented by the size of the value
        /// </remarks>
        public ushort ReadUInt16(ref int start)
        {
            Span<byte> bytes = stackalloc byte[sizeof(ushort)];
            ReadSpanAt(ref start, bytes);
            return _littleEndian ? BinaryPrimitives.ReadUInt16LittleEndian(bytes) : BinaryPrimitives.ReadUInt16BigEndian(bytes);
        }

        // <summary>
        /// Extracts byte from the image byte array
        /// </summary>
        /// <param name="start">Start index of the value</param>
        /// <remarks>
        /// The <paramref name="start"/> gets incremented by the size of the value
        /// </remarks>
        public byte ReadByte(ref int start)
        {
            Span<byte> bytes = stackalloc byte[sizeof(byte)];
            ReadSpanAt(ref start, bytes);
            return bytes[0];
        }

        // <summary>
        /// Extracts bits from the image byte array
        /// </summary>
        /// <param name="numBits">Number of bits to read</param>
        /// <param name="bitOffset">Start bit of the value</param>
        /// <remarks>
        /// The <paramref name="bitOffset"/> gets incremented by <paramref name="numBits">
        /// </remarks>
        public int ReadBits(int numBits, ref int bitOffset)
        {
            int start = bitOffset / BITS_PER_BYTE;
            int bits = bitOffset % BITS_PER_BYTE;
            int val = ReadByte(ref start) >> bits;
            bits += numBits;
            while (bits > BITS_PER_BYTE)
            {
                bits -= BITS_PER_BYTE;
                if (bits > 0)
                {
                    int extraBits = ReadByte(ref start) << (numBits - bits);
                    val ^= extraBits;
                }
            }
            val &= (1 << numBits) - 1;
            bitOffset += numBits;
            return val;
        }

        // <summary>
        /// Decode variable length numbers
        /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/gcinfodecoder.h">src\inc\gcinfodecoder.h</a> DecodeVarLengthUnsigned
        /// </summary>
        /// <param name="len">Number of bits to read</param>
        /// <param name="bitOffset">Start bit of the value</param>
        /// <remarks>
        /// The <paramref name="bitOffset"/> gets incremented by the size of the value
        /// </remarks>
        public uint DecodeVarLengthUnsigned(int len, ref int bitOffset)
        {
            uint numEncodings = (uint)(1 << len);
            uint result = 0;
            for (int shift = 0; ; shift += len)
            {
                uint currentChunk = (uint)ReadBits(len + 1, ref bitOffset);
                result |= (currentChunk & (numEncodings - 1)) << shift;
                if ((currentChunk & numEncodings) == 0)
                {
                    // Extension bit is not set, we're done.
                    return result;
                }
            }
        }

        // <summary>
        /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/gcinfodecoder.h">src\inc\gcinfodecoder.h</a> DecodeVarLengthSigned
        /// </summary>
        public int DecodeVarLengthSigned(int len, ref int bitOffset)
        {
            int numEncodings = (1 << len);
            int result = 0;
            for (int shift = 0; ; shift += len)
            {
                int currentChunk = ReadBits(len + 1, ref bitOffset);
                result |= (currentChunk & (numEncodings - 1)) << shift;
                if ((currentChunk & numEncodings) == 0)
                {
                    // Extension bit is not set, sign-extend and we're done.
                    int sbits = BITS_PER_SIZE_T - (shift + len);
                    result <<= sbits;
                    result >>= sbits;   // This provides the sign extension
                    return result;
                }
            }
        }

        // <summary>
        /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/nativeformatreader.h">src\vm\nativeformatreader.h</a> DecodeUnsigned
        /// </summary>
        public uint DecodeUnsigned(uint offset, ref uint pValue)
        {
            if (offset >= _backingStream.Length)
                throw new System.BadImageFormatException("offset out of bounds");

            int off = (int)offset;
            uint val = ReadByte(ref off);

            if ((val & 1) == 0)
            {
                pValue = (val >> 1);
                offset += 1;
            }
            else if ((val & 2) == 0)
            {
                if (offset + 1 >= _backingStream.Length)
                    throw new System.BadImageFormatException("offset out of bounds");

                pValue = (val >> 2) |
                        ((uint)ReadByte(ref off) << 6);
                offset += 2;
            }
            else if ((val & 4) == 0)
            {
                if (offset + 2 >= _backingStream.Length)
                    throw new System.BadImageFormatException("offset out of bounds");

                pValue = (val >> 3) |
                        ((uint)ReadByte(ref off) << 5) |
                        ((uint)ReadByte(ref off) << 13);
                offset += 3;
            }
            else if ((val & 8) == 0)
            {
                if (offset + 3 >= _backingStream.Length)
                    throw new System.BadImageFormatException("offset out of bounds");

                pValue = (val >> 4) |
                        ((uint)ReadByte(ref off) << 4) |
                        ((uint)ReadByte(ref off) << 12) |
                        ((uint)ReadByte(ref off) << 20);
                offset += 4;
            }
            else if ((val & 16) == 0)
            {
                pValue = ReadUInt32(ref off);
                offset += 5;
            }
            else
            {
                throw new System.BadImageFormatException("DecodeUnsigned");
            }

            return offset;
        }

        // <summary>
        /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/nativeformatreader.h">src\vm\nativeformatreader.h</a> DecodeSigned
        /// </summary>
        public uint DecodeSigned(uint offset, ref int pValue)
        {
            if (offset >= _backingStream.Length)
                throw new System.BadImageFormatException("offset out of bounds");

            int off = (int)offset;
            int val = ReadByte(ref off);

            if ((val & 1) == 0)
            {
                pValue = (val >> 1);
                offset += 1;
            }
            else if ((val & 2) == 0)
            {
                if (offset + 1 >= _backingStream.Length)
                    throw new System.BadImageFormatException("offset out of bounds");

                pValue = (val >> 2) |
                        (ReadByte(ref off) << 6);
                offset += 2;
            }
            else if ((val & 4) == 0)
            {
                if (offset + 2 >= _backingStream.Length)
                    throw new System.BadImageFormatException("offset out of bounds");

                pValue = (val >> 3) |
                        (ReadByte(ref off) << 5) |
                        (ReadByte(ref off) << 13);
                offset += 3;
            }
            else if ((val & 8) == 0)
            {
                if (offset + 3 >= _backingStream.Length)
                    throw new System.BadImageFormatException("offset out of bounds");

                pValue = (val >> 4) |
                        (ReadByte(ref off) << 4) |
                        (ReadByte(ref off) << 12) |
                        (ReadByte(ref off) << 20);
                offset += 4;
            }
            else if ((val & 16) == 0)
            {
                pValue = ReadInt32(ref off);
                offset += 5;
            }
            else
            {
                throw new System.BadImageFormatException("DecodeSigned");
            }

            return offset;
        }

        // <summary>
        /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/debug/daccess/nidump.cpp">src\debug\daccess\nidump.cpp</a> DacSigUncompressData and DacSigUncompressBigData
        /// </summary>
        public uint ReadCompressedData(ref int start)
        {
            int off = start;
            uint data = ReadUInt32(ref off);
            if ((data & 0x80) == 0x00)
            {
                start++;
                return (byte)data;
            }
            if ((data & 0xC0) == 0x80)  // 10?? ????
            {
                data = (uint)((ReadByte(ref start) & 0x3f) << 8);
                data |= ReadByte(ref start);
            }
            else // 110? ????
            {
                data = (uint)(ReadByte(ref start) & 0x1f) << 24;
                data |= (uint)ReadByte(ref start) << 16;
                data |= (uint)ReadByte(ref start) << 8;
                data |= ReadByte(ref start);
            }
            return data;
        }

        /// <summary>
        /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/gcdecoder.cpp">src\inc\gcdecoder.cpp</a> decodeUnsigned
        /// </summary>
        public uint DecodeUnsignedGc(ref int start)
        {
            int size = 1;
            byte data = ReadByte(ref start);
            uint value = (uint)data & 0x7f;
            while ((data & 0x80) != 0)
            {
                size++;
                data = ReadByte(ref start);
                value <<= 7;
                value += (uint)data & 0x7f;
            }
            return value;
        }

        /// <summary>
        /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/gcdecoder.cpp">src\inc\gcdecoder.cpp</a> decodeSigned
        /// </summary>
        public int DecodeSignedGc(ref int start)
        {
            int size = 1;
            byte data = ReadByte(ref start);
            byte first = data;
            int value = data & 0x3f;
            while ((data & 0x80) != 0)
            {
                size++;
                data = ReadByte(ref start);
                value <<= 7;
                value += data & 0x7f;
            }
            if ((first & 0x40) != 0)
                value = -value;

            return value;
        }

        /// <summary>
        /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/gcdecoder.cpp">src\inc\gcdecoder.cpp</a> decodeUDelta
        /// </summary>
        public uint DecodeUDelta(ref int start, uint lastValue)
        {
            uint delta = DecodeUnsignedGc(ref start);
            return lastValue + delta;
        }
    }
}
