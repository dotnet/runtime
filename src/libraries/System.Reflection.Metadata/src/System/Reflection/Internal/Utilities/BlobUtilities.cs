// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Internal;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection
{
    internal static unsafe class BlobUtilities
    {
        public static byte[] ReadBytes(byte* buffer, int byteCount)
        {
            if (byteCount == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] result = new byte[byteCount];
            Marshal.Copy((IntPtr)buffer, result, 0, byteCount);
            return result;
        }

        public static ImmutableArray<byte> ReadImmutableBytes(byte* buffer, int byteCount)
        {
            byte[]? bytes = ReadBytes(buffer, byteCount);
            return ImmutableByteArrayInterop.DangerousCreateFromUnderlyingArray(ref bytes);
        }

        public static void WriteBytes(this byte[] buffer, int start, byte value, int byteCount)
        {
            Debug.Assert(buffer.Length > 0);

            fixed (byte* bufferPtr = &buffer[0])
            {
                byte* startPtr = bufferPtr + start;
                for (int i = 0; i < byteCount; i++)
                {
                    startPtr[i] = value;
                }
            }
        }

        public static void WriteDouble(this byte[] buffer, int start, double value)
        {
            WriteUInt64(buffer, start, *(ulong*)&value);
        }

        public static void WriteSingle(this byte[] buffer, int start, float value)
        {
            WriteUInt32(buffer, start, *(uint*)&value);
        }

        public static void WriteByte(this byte[] buffer, int start, byte value)
        {
            // Perf: The compiler emits a check when pinning the buffer. It's thus not worth doing so.
            buffer[start] = value;
        }

        public static void WriteUInt16(this byte[] buffer, int start, ushort value)
        {
            fixed (byte* ptr = &buffer[start])
            {
                unchecked
                {
                    ptr[0] = (byte)value;
                    ptr[1] = (byte)(value >> 8);
                }
            }
        }

        public static void WriteUInt16BE(this byte[] buffer, int start, ushort value)
        {
            fixed (byte* ptr = &buffer[start])
            {
                unchecked
                {
                    ptr[0] = (byte)(value >> 8);
                    ptr[1] = (byte)value;
                }
            }
        }

        public static void WriteUInt32BE(this byte[] buffer, int start, uint value)
        {
            fixed (byte* ptr = &buffer[start])
            {
                unchecked
                {
                    ptr[0] = (byte)(value >> 24);
                    ptr[1] = (byte)(value >> 16);
                    ptr[2] = (byte)(value >> 8);
                    ptr[3] = (byte)value;
                }
            }
        }

        public static void WriteUInt32(this byte[] buffer, int start, uint value)
        {
            fixed (byte* ptr = &buffer[start])
            {
                unchecked
                {
                    ptr[0] = (byte)value;
                    ptr[1] = (byte)(value >> 8);
                    ptr[2] = (byte)(value >> 16);
                    ptr[3] = (byte)(value >> 24);
                }
            }
        }

        public static void WriteUInt64(this byte[] buffer, int start, ulong value)
        {
            WriteUInt32(buffer, start, unchecked((uint)value));
            WriteUInt32(buffer, start + 4, unchecked((uint)(value >> 32)));
        }

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

        public static void WriteUTF8(this byte[] buffer, int start, char* charPtr, int charCount, int byteCount, bool allowUnpairedSurrogates)
        {
            Debug.Assert(byteCount >= charCount);
            const char ReplacementCharacter = '\uFFFD';

            char* strEnd = charPtr + charCount;
            fixed (byte* bufferPtr = &buffer[0])
            {
                byte* ptr = bufferPtr + start;

                if (byteCount == charCount)
                {
                    while (charPtr < strEnd)
                    {
                        Debug.Assert(*charPtr <= 0x7f);
                        *ptr++ = unchecked((byte)*charPtr++);
                    }
                }
                else
                {
                    while (charPtr < strEnd)
                    {
                        char c = *charPtr++;

                        if (c < 0x80)
                        {
                            *ptr++ = (byte)c;
                            continue;
                        }

                        if (c < 0x800)
                        {
                            ptr[0] = (byte)(((c >> 6) & 0x1F) | 0xC0);
                            ptr[1] = (byte)((c & 0x3F) | 0x80);
                            ptr += 2;
                            continue;
                        }

                        if (IsSurrogateChar(c))
                        {
                            // surrogate pair
                            if (IsHighSurrogateChar(c) && charPtr < strEnd && IsLowSurrogateChar(*charPtr))
                            {
                                int highSurrogate = c;
                                int lowSurrogate = *charPtr++;
                                int codepoint = (((highSurrogate - 0xd800) << 10) + lowSurrogate - 0xdc00) + 0x10000;
                                ptr[0] = (byte)(((codepoint >> 18) & 0x7) | 0xF0);
                                ptr[1] = (byte)(((codepoint >> 12) & 0x3F) | 0x80);
                                ptr[2] = (byte)(((codepoint >> 6) & 0x3F) | 0x80);
                                ptr[3] = (byte)((codepoint & 0x3F) | 0x80);
                                ptr += 4;
                                continue;
                            }

                            // unpaired high/low surrogate
                            if (!allowUnpairedSurrogates)
                            {
                                c = ReplacementCharacter;
                            }
                        }

                        ptr[0] = (byte)(((c >> 12) & 0xF) | 0xE0);
                        ptr[1] = (byte)(((c >> 6) & 0x3F) | 0x80);
                        ptr[2] = (byte)((c & 0x3F) | 0x80);
                        ptr += 3;
                    }
                }

                Debug.Assert(ptr == bufferPtr + start + byteCount);
                Debug.Assert(charPtr == strEnd);
            }
        }

        internal static unsafe int GetUTF8ByteCount(string str)
        {
            fixed (char* ptr = str)
            {
                return GetUTF8ByteCount(ptr, str.Length);
            }
        }

        internal static unsafe int GetUTF8ByteCount(char* str, int charCount)
        {
            return GetUTF8ByteCount(str, charCount, int.MaxValue, out _);
        }

        internal static int GetUTF8ByteCount(char* str, int charCount, int byteLimit, out char* remainder)
        {
            char* end = str + charCount;

            char* ptr = str;
            int byteCount = 0;
            while (ptr < end)
            {
                int characterSize;
                char c = *ptr++;
                if (c < 0x80)
                {
                    characterSize = 1;
                }
                else if (c < 0x800)
                {
                    characterSize = 2;
                }
                else if (IsHighSurrogateChar(c) && ptr < end && IsLowSurrogateChar(*ptr))
                {
                    // surrogate pair:
                    characterSize = 4;
                    ptr++;
                }
                else
                {
                    characterSize = 3;
                }

                if (byteCount + characterSize > byteLimit)
                {
                    ptr -= (characterSize < 4) ? 1 : 2;
                    break;
                }

                byteCount += characterSize;
            }

            remainder = ptr;
            return byteCount;
        }

        internal static bool IsSurrogateChar(int c)
        {
            return unchecked((uint)(c - 0xD800)) <= 0xDFFF - 0xD800;
        }

        internal static bool IsHighSurrogateChar(int c)
        {
            return unchecked((uint)(c - 0xD800)) <= 0xDBFF - 0xD800;
        }

        internal static bool IsLowSurrogateChar(int c)
        {
            return unchecked((uint)(c - 0xDC00)) <= 0xDFFF - 0xDC00;
        }

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
