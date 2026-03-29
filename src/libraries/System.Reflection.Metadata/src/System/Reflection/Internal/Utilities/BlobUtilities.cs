// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Reflection.Internal;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
#if NET
using System.Text.Unicode;
#endif

namespace System.Reflection
{
    internal static class BlobUtilities
    {
        public static void WriteBytes(this byte[] buffer, int start, byte value, int byteCount)
        {
            Debug.Assert(buffer.Length > 0);

            new Span<byte>(buffer, start, byteCount).Fill(value);
        }

        public static void WriteDouble(this byte[] buffer, int start, double value)
        {
#if NET
            WriteUInt64(buffer, start, BitConverter.DoubleToUInt64Bits(value));
#else
            unsafe
            {
                WriteUInt64(buffer, start, *(ulong*)&value);
            }
#endif
        }

        public static void WriteSingle(this byte[] buffer, int start, float value)
        {
#if NET
            WriteUInt32(buffer, start, BitConverter.SingleToUInt32Bits(value));
#else
            unsafe
            {
                WriteUInt32(buffer, start, *(uint*)&value);
            }
#endif
        }

        public static void WriteByte(this byte[] buffer, int start, byte value)
        {
            // Perf: The compiler emits a check when pinning the buffer. It's thus not worth doing so.
            buffer[start] = value;
        }

        public static void WriteUInt16(this byte[] buffer, int start, ushort value) =>
            Unsafe.WriteUnaligned(ref buffer[start], !BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);

        public static void WriteUInt16BE(this byte[] buffer, int start, ushort value) =>
            Unsafe.WriteUnaligned(ref buffer[start], BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);

        public static void WriteUInt32BE(this byte[] buffer, int start, uint value) =>
            Unsafe.WriteUnaligned(ref buffer[start], BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);

        public static void WriteUInt32(this byte[] buffer, int start, uint value) =>
            Unsafe.WriteUnaligned(ref buffer[start], !BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);

        public static void WriteUInt64(this byte[] buffer, int start, ulong value) =>
            Unsafe.WriteUnaligned(ref buffer[start], !BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);

        public const int SizeOfSerializedDecimal = sizeof(byte) + 3 * sizeof(uint);

        public static void WriteDecimal(this byte[] buffer, int start, decimal value)
        {
            bool isNegative;
            byte scale;
            uint low, mid, high;
            value.GetBits(out isNegative, out scale, out low, out mid, out high);

            WriteByte(buffer, start, (byte)(scale | (isNegative ? 0x80 : 0x00)));
            WriteUInt32(buffer, start + 1, low);
            WriteUInt32(buffer, start + 5, mid);
            WriteUInt32(buffer, start + 9, high);
        }

        public const int SizeOfGuid = 16;

        public static void WriteGuid(this byte[] buffer, int start, Guid value)
        {
#if NET
            bool written = value.TryWriteBytes(buffer.AsSpan(start));
            // This function is not public, callers have to ensure that enough space is available.
            Debug.Assert(written);
#else
            unsafe
            {
                fixed (byte* dst = &buffer[start])
                {
                    byte* src = (byte*)&value;

                    uint a = *(uint*)(src + 0);
                    unchecked
                    {
                        dst[0] = (byte)a;
                        dst[1] = (byte)(a >> 8);
                        dst[2] = (byte)(a >> 16);
                        dst[3] = (byte)(a >> 24);

                        ushort b = *(ushort*)(src + 4);
                        dst[4] = (byte)b;
                        dst[5] = (byte)(b >> 8);

                        ushort c = *(ushort*)(src + 6);
                        dst[6] = (byte)c;
                        dst[7] = (byte)(c >> 8);
                    }

                    dst[8] = src[8];
                    dst[9] = src[9];
                    dst[10] = src[10];
                    dst[11] = src[11];
                    dst[12] = src[12];
                    dst[13] = src[13];
                    dst[14] = src[14];
                    dst[15] = src[15];
                }
            }
#endif
        }

#if NET
        public static void WriteUtf8(ReadOnlySpan<char> source, Span<byte> destination, out int charsRead, out int bytesWritten, bool allowUnpairedSurrogates)
        {
            int sourceLength = source.Length;
            int destinationLength = destination.Length;

            while (true)
            {
                OperationStatus status = Utf8.FromUtf16(source, destination, out int consumed, out int written, replaceInvalidSequences: !allowUnpairedSurrogates, isFinalBlock: true);
                source = source.Slice(consumed);
                destination = destination.Slice(written);

                if (status <= OperationStatus.DestinationTooSmall)
                {
                    break;
                }

                // NeedsMoreData is not expected because isFinalBlock is set to true.
                Debug.Assert(status == OperationStatus.InvalidData);
                // If we don't allow unpaired surrogates, they should have been replaced by FromUtf16.
                Debug.Assert(allowUnpairedSurrogates);
                char c = source[0];
                Debug.Assert(char.IsSurrogate(c));
                if (destination.Length < 3)
                {
                    break;
                }
                destination[0] = (byte)(((c >> 12) & 0xF) | 0xE0);
                destination[1] = (byte)(((c >> 6) & 0x3F) | 0x80);
                destination[2] = (byte)((c & 0x3F) | 0x80);
                source = source.Slice(1);
                destination = destination.Slice(3);
            }

            charsRead = sourceLength - source.Length;
            bytesWritten = destinationLength - destination.Length;
        }
#else
        public static void WriteUtf8(ReadOnlySpan<char> source, Span<byte> destination, out int charsRead, out int bytesWritten, bool allowUnpairedSurrogates)
        {
            const char ReplacementCharacter = '\uFFFD';

            int sourceLength = source.Length;
            int destinationLength = destination.Length;

            unsafe
            {
                fixed (char* pSource = &MemoryMarshal.GetReference(source))
                fixed (byte* pDestination = &MemoryMarshal.GetReference(destination))
                {
                    char* src = pSource, srcEnd = pSource + source.Length;
                    byte* dst = pDestination, dstEnd = pDestination + destination.Length;

                    while (src < srcEnd)
                    {
                        char c = *src;
                        if (c < 0x80)
                        {
                            if (dstEnd - dst < 1)
                            {
                                break;
                            }
                            *dst++ = (byte)c;
                            src++;
                        }
                        else if (c < 0x800)
                        {
                            if (dstEnd - dst < 2)
                            {
                                break;
                            }
                            *dst++ = (byte)((c >> 6) | 0xC0);
                            *dst++ = (byte)((c & 0x3F) | 0x80);
                            src++;
                        }
                        else
                        {
                            if (char.IsSurrogate(c))
                            {
                                // surrogate pair
                                if (char.IsHighSurrogate(c) && srcEnd - src >= 2 && src[1] is char cLow && char.IsLowSurrogate(cLow))
                                {
                                    if (dstEnd - dst < 4)
                                    {
                                        break;
                                    }
                                    int codepoint = ((c - 0xd800) << 10) + cLow - 0xdc00 + 0x10000;
                                    *dst++ = (byte)((codepoint >> 18) | 0xF0);
                                    *dst++ = (byte)(((codepoint >> 12) & 0x3F) | 0x80);
                                    *dst++ = (byte)(((codepoint >> 6) & 0x3F) | 0x80);
                                    *dst++ = (byte)((codepoint & 0x3F) | 0x80);
                                    src += 2;
                                    continue;
                                }

                                // unpaired high/low surrogate
                                if (!allowUnpairedSurrogates)
                                {
                                    c = ReplacementCharacter;
                                }
                            }

                            if (dstEnd - dst < 3)
                            {
                                break;
                            }
                            *dst++ = (byte)((c >> 12) | 0xE0);
                            *dst++ = (byte)(((c >> 6) & 0x3F) | 0x80);
                            *dst++ = (byte)((c & 0x3F) | 0x80);
                            src++;
                        }
                    }

                    charsRead = (int)(src - pSource);
                    bytesWritten = (int)(dst - pDestination);
                }
            }
        }
#endif

#if !NET
        internal static unsafe int GetByteCount(this Encoding encoding, ReadOnlySpan<char> str)
        {
            fixed (char* ptr = &MemoryMarshal.GetReference(str))
            {
                return encoding.GetByteCount(ptr, str.Length);
            }
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ValidateRange(int bufferLength, int start, int byteCount, string byteCountParameterName)
        {
            if (start < 0 || start > bufferLength)
            {
                Throw.ArgumentOutOfRange(nameof(start));
            }

            if (byteCount < 0 || byteCount > bufferLength - start)
            {
                Throw.ArgumentOutOfRange(byteCountParameterName);
            }
        }

        internal static int GetUserStringByteLength(int characterCount)
        {
            return characterCount * 2 + 1;
        }

        internal static byte GetUserStringTrailingByte(string str)
        {
            // ECMA-335 II.24.2.4:
            // This final byte holds the value 1 if and only if any UTF16 character within
            // the string has any bit set in its top byte, or its low byte is any of the following:
            // 0x01-0x08, 0x0E-0x1F, 0x27, 0x2D, 0x7F.  Otherwise, it holds 0.
            // The 1 signifies Unicode characters that require handling beyond that normally provided for 8-bit encoding sets.

            foreach (char ch in str)
            {
                if (ch >= 0x7F)
                {
                    return 1;
                }

                switch ((int)ch)
                {
                    case 0x1:
                    case 0x2:
                    case 0x3:
                    case 0x4:
                    case 0x5:
                    case 0x6:
                    case 0x7:
                    case 0x8:
                    case 0xE:
                    case 0xF:
                    case 0x10:
                    case 0x11:
                    case 0x12:
                    case 0x13:
                    case 0x14:
                    case 0x15:
                    case 0x16:
                    case 0x17:
                    case 0x18:
                    case 0x19:
                    case 0x1A:
                    case 0x1B:
                    case 0x1C:
                    case 0x1D:
                    case 0x1E:
                    case 0x1F:
                    case 0x27:
                    case 0x2D:
                        return 1;
                }
            }

            return 0;
        }
    }
}
