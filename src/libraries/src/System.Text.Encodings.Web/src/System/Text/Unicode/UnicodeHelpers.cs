// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Unicode
{
    /// <summary>
    /// Contains helpers for dealing with Unicode code points.
    /// </summary>
    internal static unsafe partial class UnicodeHelpers
    {
        /// <summary>
        /// Used for invalid Unicode sequences or other unrepresentable values.
        /// </summary>
        private const char UNICODE_REPLACEMENT_CHAR = '\uFFFD';

        /// <summary>
        /// The last code point defined by the Unicode specification.
        /// </summary>
        internal const int UNICODE_LAST_CODEPOINT = 0x10FFFF;

        // This field is only used on big-endian architectures. We don't
        // bother computing it on little-endian architectures.
        private static uint[] _definedCharacterBitmapBigEndian = (BitConverter.IsLittleEndian) ? null : CreateDefinedCharacterBitmapMachineEndian();

        private static uint[] CreateDefinedCharacterBitmapMachineEndian()
        {
            Debug.Assert(!BitConverter.IsLittleEndian);

            // We need to convert little-endian to machine-endian.

            ReadOnlySpan<byte> remainingBitmap = DefinedCharsBitmapSpan;
            uint[] bigEndianData = new uint[remainingBitmap.Length / sizeof(uint)];

            for (int i = 0; i < bigEndianData.Length; i++)
            {
                bigEndianData[i] = BinaryPrimitives.ReadUInt32LittleEndian(remainingBitmap);
                remainingBitmap = remainingBitmap.Slice(sizeof(uint));
            }

            return bigEndianData;
        }

        /// <summary>
        /// Returns a bitmap of all characters which are defined per the checked-in version
        /// of the Unicode specification.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<uint> GetDefinedCharacterBitmap()
        {
            if (BitConverter.IsLittleEndian)
            {
                // Underlying data is a series of 32-bit little-endian values and is guaranteed
                // properly aligned by the compiler, so we know this is a valid cast byte -> uint.

                return MemoryMarshal.Cast<byte, uint>(DefinedCharsBitmapSpan);
            }
            else
            {
                // Static compiled data was little-endian; we had to create a big-endian
                // representation at runtime.

                return _definedCharacterBitmapBigEndian;
            }
        }

        /// <summary>
        /// Given a UTF-16 character stream, reads the next scalar value from the stream.
        /// Set 'endOfString' to true if 'pChar' points to the last character in the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetScalarValueFromUtf16(char first, char? second, out bool wasSurrogatePair)
        {
            if (!char.IsSurrogate(first))
            {
                wasSurrogatePair = false;
                return first;
            }
            return GetScalarValueFromUtf16Slow(first, second, out wasSurrogatePair);
        }

        private static int GetScalarValueFromUtf16Slow(char first, char? second, out bool wasSurrogatePair)
        {
#if DEBUG
            if (!Char.IsSurrogate(first))
            {
                Debug.Assert(false, "This case should've been handled by the fast path.");
                wasSurrogatePair = false;
                return first;
            }
#endif
            if (char.IsHighSurrogate(first))
            {
                if (second != null)
                {
                    if (char.IsLowSurrogate(second.Value))
                    {
                        // valid surrogate pair - extract codepoint
                        wasSurrogatePair = true;
                        return GetScalarValueFromUtf16SurrogatePair(first, second.Value);
                    }
                    else
                    {
                        // unmatched surrogate - substitute
                        wasSurrogatePair = false;
                        return UNICODE_REPLACEMENT_CHAR;
                    }
                }
                else
                {
                    // unmatched surrogate - substitute
                    wasSurrogatePair = false;
                    return UNICODE_REPLACEMENT_CHAR;
                }
            }
            else
            {
                // unmatched surrogate - substitute
                Debug.Assert(char.IsLowSurrogate(first));
                wasSurrogatePair = false;
                return UNICODE_REPLACEMENT_CHAR;
            }
        }

        /// <summary>
        /// Given a UTF-16 character stream, reads the next scalar value from the stream.
        /// Set 'endOfString' to true if 'pChar' points to the last character in the stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetScalarValueFromUtf16(char* pChar, bool endOfString)
        {
            // This method is marked as AggressiveInlining to handle the common case of a non-surrogate
            // character. The surrogate case is handled in the slower fallback code path.
            char thisChar = *pChar;
            return (char.IsSurrogate(thisChar)) ? GetScalarValueFromUtf16Slow(pChar, endOfString) : thisChar;
        }

        private static int GetScalarValueFromUtf16Slow(char* pChar, bool endOfString)
        {
            char firstChar = pChar[0];

            if (!char.IsSurrogate(firstChar))
            {
                Debug.Assert(false, "This case should've been handled by the fast path.");
                return firstChar;
            }
            else if (char.IsHighSurrogate(firstChar))
            {
                if (endOfString)
                {
                    // unmatched surrogate - substitute
                    return UNICODE_REPLACEMENT_CHAR;
                }
                else
                {
                    char secondChar = pChar[1];
                    if (char.IsLowSurrogate(secondChar))
                    {
                        // valid surrogate pair - extract codepoint
                        return GetScalarValueFromUtf16SurrogatePair(firstChar, secondChar);
                    }
                    else
                    {
                        // unmatched surrogate - substitute
                        return UNICODE_REPLACEMENT_CHAR;
                    }
                }
            }
            else
            {
                // unmatched surrogate - substitute
                Debug.Assert(char.IsLowSurrogate(firstChar));
                return UNICODE_REPLACEMENT_CHAR;
            }
        }

        private static int GetScalarValueFromUtf16SurrogatePair(char highSurrogate, char lowSurrogate)
        {
            Debug.Assert(char.IsHighSurrogate(highSurrogate));
            Debug.Assert(char.IsLowSurrogate(lowSurrogate));

            // See http://www.unicode.org/versions/Unicode6.2.0/ch03.pdf, Table 3.5 for the
            // details of this conversion. We don't use Char.ConvertToUtf32 because its exception
            // handling shows up on the hot path, and our caller has already sanitized the inputs.
            return (lowSurrogate & 0x3ff) | (((highSurrogate & 0x3ff) + (1 << 6)) << 10);
        }

        internal static void GetUtf16SurrogatePairFromAstralScalarValue(int scalar, out char highSurrogate, out char lowSurrogate)
        {
            Debug.Assert(0x10000 <= scalar && scalar <= UNICODE_LAST_CODEPOINT);

            // See http://www.unicode.org/versions/Unicode6.2.0/ch03.pdf, Table 3.5 for the
            // details of this conversion. We don't use Char.ConvertFromUtf32 because its exception
            // handling shows up on the hot path, it allocates temporary strings (which we don't want),
            // and our caller has already sanitized the inputs.

            int x = scalar & 0xFFFF;
            int u = scalar >> 16;
            int w = u - 1;
            highSurrogate = (char)(0xD800 | (w << 6) | (x >> 10));
            lowSurrogate = (char)(0xDC00 | (x & 0x3FF));
        }

        /// <summary>
        /// Given a Unicode scalar value, returns the UTF-8 representation of the value.
        /// The return value's bytes should be popped from the LSB.
        /// </summary>
        internal static int GetUtf8RepresentationForScalarValue(uint scalar)
        {
            Debug.Assert(scalar <= UNICODE_LAST_CODEPOINT);

            // See http://www.unicode.org/versions/Unicode6.2.0/ch03.pdf, Table 3.6 for the
            // details of this conversion. We don't use UTF8Encoding since we're encoding
            // a scalar code point, not a UTF16 character sequence.
            if (scalar <= 0x7f)
            {
                // one byte used: scalar 00000000 0xxxxxxx -> byte sequence 0xxxxxxx
                byte firstByte = (byte)scalar;
                return firstByte;
            }
            else if (scalar <= 0x7ff)
            {
                // two bytes used: scalar 00000yyy yyxxxxxx -> byte sequence 110yyyyy 10xxxxxx
                byte firstByte = (byte)(0xc0 | (scalar >> 6));
                byte secondByteByte = (byte)(0x80 | (scalar & 0x3f));
                return ((secondByteByte << 8) | firstByte);
            }
            else if (scalar <= 0xffff)
            {
                // three bytes used: scalar zzzzyyyy yyxxxxxx -> byte sequence 1110zzzz 10yyyyyy 10xxxxxx
                byte firstByte = (byte)(0xe0 | (scalar >> 12));
                byte secondByte = (byte)(0x80 | ((scalar >> 6) & 0x3f));
                byte thirdByte = (byte)(0x80 | (scalar & 0x3f));
                return ((((thirdByte << 8) | secondByte) << 8) | firstByte);
            }
            else
            {
                // four bytes used: scalar 000uuuuu zzzzyyyy yyxxxxxx -> byte sequence 11110uuu 10uuzzzz 10yyyyyy 10xxxxxx
                byte firstByte = (byte)(0xf0 | (scalar >> 18));
                byte secondByte = (byte)(0x80 | ((scalar >> 12) & 0x3f));
                byte thirdByte = (byte)(0x80 | ((scalar >> 6) & 0x3f));
                byte fourthByte = (byte)(0x80 | (scalar & 0x3f));
                return ((((((fourthByte << 8) | thirdByte) << 8) | secondByte) << 8) | firstByte);
            }
        }

        /// <summary>
        /// Returns a value stating whether a character is defined per the checked-in version
        /// of the Unicode specification. Certain classes of characters (control chars,
        /// private use, surrogates, some whitespace) are considered "undefined" for
        /// our purposes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsCharacterDefined(char c)
        {
            uint codePoint = (uint)c;
            int index = (int)(codePoint >> 5);
            int offset = (int)(codePoint & 0x1FU);
            return ((GetDefinedCharacterBitmap()[index] >> offset) & 0x1U) != 0;
        }

        /// <summary>
        /// Determines whether the given scalar value is in the supplementary plane and thus
        /// requires 2 characters to be represented in UTF-16 (as a surrogate pair).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsSupplementaryCodePoint(int scalar)
        {
            return ((scalar & ~((int)char.MaxValue)) != 0);
        }
    }
}
