// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Unicode
{
    /// <summary>
    /// Contains helpers for dealing with Unicode code points.
    /// </summary>
    internal static partial class UnicodeHelpers
    {
        /// <summary>
        /// The last code point defined by the Unicode specification.
        /// </summary>
        internal const int UNICODE_LAST_CODEPOINT = 0x10FFFF;

        /// <summary>
        /// Returns a bitmap of all BMP code points as a series of little-endian 32-bit values.
        /// On other-endian architectures, caller must convert each 32-bit integer to native endianness
        /// before using the data.
        /// </summary>
        internal static ReadOnlySpan<byte> GetDefinedBmpCodePointsBitmapLittleEndian() => DefinedCharsBitmapSpan;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void GetUtf16SurrogatePairFromAstralScalarValue(uint scalar, out char highSurrogate, out char lowSurrogate)
        {
            Debug.Assert(0x10000 <= scalar && scalar <= UNICODE_LAST_CODEPOINT);

            UnicodeDebug.AssertIsValidSupplementaryPlaneScalar(scalar);

            // This calculation comes from the Unicode specification, Table 3-5.

            highSurrogate = (char)((scalar + ((0xD800u - 0x40u) << 10)) >> 10);
            lowSurrogate = (char)((scalar & 0x3FFu) + 0xDC00u);
        }

        /// <summary>
        /// Given a Unicode scalar value, returns the UTF-8 representation of the value.
        /// The return value's bytes should be popped from the LSB.
        /// </summary>
        internal static int GetUtf8RepresentationForScalarValue(uint scalar)
        {
            Debug.Assert(scalar <= UNICODE_LAST_CODEPOINT);

            // See https://www.unicode.org/versions/Unicode6.2.0/ch03.pdf, Table 3.6 for the
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
