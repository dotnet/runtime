// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        /// <summary>
        /// Parses a Guid at the start of a Utf8 string.
        /// </summary>
        /// <param name="text">The Utf8 string to parse</param>
        /// <param name="value">Receives the parsed value</param>
        /// <param name="bytesConsumed">On a successful parse, receives the length in bytes of the substring that was parsed </param>
        /// <param name="standardFormat">Expected format of the Utf8 string</param>
        /// <returns>
        /// true for success. "bytesConsumed" contains the length in bytes of the substring that was parsed.
        /// false if the string was not syntactically valid or an overflow or underflow occurred. "bytesConsumed" is set to 0. 
        /// </returns>
        /// <remarks>
        /// Formats supported:
        ///     D (default)     nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn
        ///     B               {nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn}
        ///     P               (nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn)
        ///     N               nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryParse(ReadOnlySpan<byte> text, out Guid value, out int bytesConsumed, char standardFormat = default)
        {
            switch (standardFormat)
            {
                case (default):
                case 'D':
                    return TryParseGuidCore(text, false, ' ', ' ', out value, out bytesConsumed);
                case 'B':
                    return TryParseGuidCore(text, true, '{', '}', out value, out bytesConsumed);
                case 'P':
                    return TryParseGuidCore(text, true, '(', ')', out value, out bytesConsumed);
                case 'N':
                    return TryParseGuidN(text, out value, out bytesConsumed);
                default:
                    return ThrowHelper.TryParseThrowFormatException(out value, out bytesConsumed);
            }
        }

        // nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn (not very Guid-like, but the format is what it is...)
        private static bool TryParseGuidN(ReadOnlySpan<byte> text, out Guid value, out int bytesConsumed)
        {
            if (text.Length < 32)
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            if (!TryParseUInt32X(text.Slice(0, 8), out uint i1, out int justConsumed) || justConsumed != 8)
            {
                value = default;
                bytesConsumed = 0;
                return false; // 8 digits
            }

            if (!TryParseUInt16X(text.Slice(8, 4), out ushort i2, out justConsumed) || justConsumed != 4)
            {
                value = default;
                bytesConsumed = 0;
                return false; // next 4 digits
            }

            if (!TryParseUInt16X(text.Slice(12, 4), out ushort i3, out justConsumed) || justConsumed != 4)
            {
                value = default;
                bytesConsumed = 0;
                return false; // next 4 digits
            }

            if (!TryParseUInt16X(text.Slice(16, 4), out ushort i4, out justConsumed) || justConsumed != 4)
            {
                value = default;
                bytesConsumed = 0;
                return false; // next 4 digits
            }

            if (!TryParseUInt64X(text.Slice(20), out ulong i5, out justConsumed) || justConsumed != 12)
            {
                value = default;
                bytesConsumed = 0;
                return false; // next 4 digits
            }

            bytesConsumed = 32;
            value = new Guid((int)i1, (short)i2, (short)i3, (byte)(i4 >> 8), (byte)i4,
                (byte)(i5 >> 40), (byte)(i5 >> 32), (byte)(i5 >> 24), (byte)(i5 >> 16), (byte)(i5 >> 8), (byte)i5);
            return true;
        }

        // {8-4-4-4-12}, where number is the number of hex digits, and {/} are ends.
        private static bool TryParseGuidCore(ReadOnlySpan<byte> text, bool ends, char begin, char end, out Guid value, out int bytesConsumed)
        {
            int expectedCodingUnits = 36 + (ends ? 2 : 0); // 32 hex digits + 4 delimiters + 2 optional ends

            if (text.Length < expectedCodingUnits)
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            if (ends)
            {
                if (text[0] != begin)
                {
                    value = default;
                    bytesConsumed = 0;
                    return false;
                }

                text = text.Slice(1); // skip begining
            }

            if (!TryParseUInt32X(text, out uint i1, out int justConsumed))
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            if (justConsumed != 8)
            {
                value = default;
                bytesConsumed = 0;
                return false; // 8 digits
            }

            if (text[justConsumed] != '-')
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            text = text.Slice(9); // justConsumed + 1 for delimiter

            if (!TryParseUInt16X(text, out ushort i2, out justConsumed))
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            if (justConsumed != 4)
            {
                value = default;
                bytesConsumed = 0;
                return false; // 4 digits
            }

            if (text[justConsumed] != '-')
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            text = text.Slice(5); // justConsumed + 1 for delimiter

            if (!TryParseUInt16X(text, out ushort i3, out justConsumed))
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            if (justConsumed != 4)
            {
                value = default;
                bytesConsumed = 0;
                return false; // 4 digits
            }

            if (text[justConsumed] != '-')
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            text = text.Slice(5); // justConsumed + 1 for delimiter

            if (!TryParseUInt16X(text, out ushort i4, out justConsumed))
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            if (justConsumed != 4)
            {
                value = default;
                bytesConsumed = 0;
                return false; // 4 digits
            }

            if (text[justConsumed] != '-')
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            text = text.Slice(5);// justConsumed + 1 for delimiter

            if (!TryParseUInt64X(text, out ulong i5, out justConsumed))
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            if (justConsumed != 12)
            {
                value = default;
                bytesConsumed = 0;
                return false; // 12 digits
            }

            if (ends && text[justConsumed] != end)
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            bytesConsumed = expectedCodingUnits;
            value = new Guid((int)i1, (short)i2, (short)i3, (byte)(i4 >> 8), (byte)i4,
                (byte)(i5 >> 40), (byte)(i5 >> 32), (byte)(i5 >> 24), (byte)(i5 >> 16), (byte)(i5 >> 8), (byte)i5);

            return true;
        }
    }
}
