// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers.Text
{
    // All the helper methods in this class assume that the by-ref is valid and that there is
    // enough space to fit the items that will be written into the underlying memory. The calling
    // code must have already done all the necessary validation.
    internal static partial class FormattingHelpers
    {
        // A simple lookup table for converting numbers to hex.
        internal const string HexTableLower = "0123456789abcdef";

        internal const string HexTableUpper = "0123456789ABCDEF";

        /// <summary>
        /// Returns the symbol contained within the standard format. If the standard format
        /// has not been initialized, returns the provided fallback symbol.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char GetSymbolOrDefault(in StandardFormat format, char defaultSymbol)
        {
            // This is equivalent to the line below, but it is written in such a way
            // that the JIT is able to perform more optimizations.
            //
            // return (format.IsDefault) ? defaultSymbol : format.Symbol;

            var symbol = format.Symbol;
            if (symbol == default && format.Precision == default)
            {
                symbol = defaultSymbol;
            }
            return symbol;
        }

        #region UTF-8 Helper methods

        /// <summary>
        /// Fills a buffer with the ASCII character '0' (0x30).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillWithAsciiZeros(Span<byte> buffer)
        {
            // This is a faster implementation of Span<T>.Fill().
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)'0';
            }
        }

        public enum HexCasing : uint
        {
            // Output [ '0' .. '9' ] and [ 'A' .. 'F' ].
            Uppercase = 0,

            // Output [ '0' .. '9' ] and [ 'a' .. 'f' ].
            // This works because values in the range [ 0x30 .. 0x39 ] ([ '0' .. '9' ])
            // already have the 0x20 bit set, so ORing them with 0x20 is a no-op,
            // while outputs in the range [ 0x41 .. 0x46 ] ([ 'A' .. 'F' ])
            // don't have the 0x20 bit set, so ORing them maps to
            // [ 0x61 .. 0x66 ] ([ 'a' .. 'f' ]), which is what we want.
            Lowercase = 0x2020U,
        }

        // The JIT can elide bounds checks if 'startingIndex' is constant and if the caller is
        // writing to a span of known length (or the caller has already checked the bounds of the
        // furthest access).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteHexByte(byte value, Span<byte> buffer, int startingIndex = 0, HexCasing casing = HexCasing.Uppercase)
        {
            // We want to pack the incoming byte into a single integer [ 0000 HHHH 0000 LLLL ],
            // where HHHH and LLLL are the high and low nibbles of the incoming byte. Then
            // subtract this integer from a constant minuend as shown below.
            //
            //   [ 1000 1001 1000 1001 ]
            // - [ 0000 HHHH 0000 LLLL ]
            // =========================
            //   [ *YYY **** *ZZZ **** ]
            //
            // The end result of this is that YYY is 0b000 if HHHH <= 9, and YYY is 0b111 if HHHH >= 10.
            // Similarly, ZZZ is 0b000 if LLLL <= 9, and ZZZ is 0b111 if LLLL >= 10.
            // (We don't care about the value of asterisked bits.)
            //
            // To turn a nibble in the range [ 0 .. 9 ] into hex, we calculate hex := nibble + 48 (ascii '0').
            // To turn a nibble in the range [ 10 .. 15 ] into hex, we calculate hex := nibble - 10 + 65 (ascii 'A').
            //                                                                => hex := nibble + 55.
            // The difference in the starting ASCII offset is (55 - 48) = 7, depending on whether the nibble is <= 9 or >= 10.
            // Since 7 is 0b111, this conveniently matches the YYY or ZZZ value computed during the earlier subtraction.

            // The commented out code below is code that directly implements the logic described above.

            //uint packedOriginalValues = (((uint)value & 0xF0U) << 4) + ((uint)value & 0x0FU);
            //uint difference = 0x8989U - packedOriginalValues;
            //uint add7Mask = (difference & 0x7070U) >> 4; // line YYY and ZZZ back up with the packed values
            //uint packedResult = packedOriginalValues + add7Mask + 0x3030U /* ascii '0' */;

            // The code below is equivalent to the commented out code above but has been tweaked
            // to allow codegen to make some extra optimizations.

            uint difference = (((uint)value & 0xF0U) << 4) + ((uint)value & 0x0FU) - 0x8989U;
            uint packedResult = ((((uint)(-(int)difference) & 0x7070U) >> 4) + difference + 0xB9B9U) | (uint)casing;

            // The low byte of the packed result contains the hex representation of the incoming byte's low nibble.
            // The adjacent byte of the packed result contains the hex representation of the incoming byte's high nibble.

            // Finally, write to the output buffer starting with the *highest* index so that codegen can
            // elide all but the first bounds check. (This only works if 'startingIndex' is a compile-time constant.)

            buffer[startingIndex + 1] = (byte)(packedResult);
            buffer[startingIndex] = (byte)(packedResult >> 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDigits(ulong value, Span<byte> buffer)
        {
            // We can mutate the 'value' parameter since it's a copy-by-value local.
            // It'll be used to represent the value left over after each division by 10.

            for (int i = buffer.Length - 1; i >= 1; i--)
            {
                ulong temp = '0' + value;
                value /= 10;
                buffer[i] = (byte)(temp - (value * 10));
            }

            Debug.Assert(value < 10);
            buffer[0] = (byte)('0' + value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDigitsWithGroupSeparator(ulong value, Span<byte> buffer)
        {
            // We can mutate the 'value' parameter since it's a copy-by-value local.
            // It'll be used to represent the value left over after each division by 10.

            int digitsWritten = 0;
            for (int i = buffer.Length - 1; i >= 1; i--)
            {
                ulong temp = '0' + value;
                value /= 10;
                buffer[i] = (byte)(temp - (value * 10));
                if (digitsWritten == Utf8Constants.GroupSize - 1)
                {
                    buffer[--i] = Utf8Constants.Comma;
                    digitsWritten = 0;
                }
                else
                {
                    digitsWritten++;
                }
            }

            Debug.Assert(value < 10);
            buffer[0] = (byte)('0' + value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDigits(uint value, Span<byte> buffer)
        {
            // We can mutate the 'value' parameter since it's a copy-by-value local.
            // It'll be used to represent the value left over after each division by 10.

            for (int i = buffer.Length - 1; i >= 1; i--)
            {
                uint temp = '0' + value;
                value /= 10;
                buffer[i] = (byte)(temp - (value * 10));
            }

            Debug.Assert(value < 10);
            buffer[0] = (byte)('0' + value);
        }

        /// <summary>
        /// Writes a value [ 0000 .. 9999 ] to the buffer starting at the specified offset.
        /// This method performs best when the starting index is a constant literal.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFourDecimalDigits(uint value, Span<byte> buffer, int startingIndex = 0)
        {
            Debug.Assert(0 <= value && value <= 9999);

            uint temp = '0' + value;
            value /= 10;
            buffer[startingIndex + 3] = (byte)(temp - (value * 10));

            temp = '0' + value;
            value /= 10;
            buffer[startingIndex + 2] = (byte)(temp - (value * 10));

            temp = '0' + value;
            value /= 10;
            buffer[startingIndex + 1] = (byte)(temp - (value * 10));

            buffer[startingIndex] = (byte)('0' + value);
        }

        /// <summary>
        /// Writes a value [ 00 .. 99 ] to the buffer starting at the specified offset.
        /// This method performs best when the starting index is a constant literal.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTwoDecimalDigits(uint value, Span<byte> buffer, int startingIndex = 0)
        {
            Debug.Assert(0 <= value && value <= 99);

            uint temp = '0' + value;
            value /= 10;
            buffer[startingIndex + 1] = (byte)(temp - (value * 10));

            buffer[startingIndex] = (byte)('0' + value);
        }

        #endregion UTF-8 Helper methods

        #region Math Helper methods

        /// <summary>
        /// We don't have access to Math.DivRem, so this is a copy of the implementation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong DivMod(ulong numerator, ulong denominator, out ulong modulo)
        {
            ulong div = numerator / denominator;
            modulo = numerator - (div * denominator);
            return div;
        }

        /// <summary>
        /// We don't have access to Math.DivRem, so this is a copy of the implementation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint DivMod(uint numerator, uint denominator, out uint modulo)
        {
            uint div = numerator / denominator;
            modulo = numerator - (div * denominator);
            return div;
        }

        #endregion Math Helper methods

        //
        // Enable use of ThrowHelper from TryFormat() routines without introducing dozens of non-code-coveraged "bytesWritten = 0; return false" boilerplate.
        //
        public static bool TryFormatThrowFormatException(out int bytesWritten)
        {
            bytesWritten = 0;
            ThrowHelper.ThrowFormatException_BadFormatSpecifier();
            return false;
        }
    }
}
