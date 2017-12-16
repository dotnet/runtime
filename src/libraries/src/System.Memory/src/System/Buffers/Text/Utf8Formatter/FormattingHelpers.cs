// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

#if !netstandard
using Internal.Runtime.CompilerServices;
#endif

namespace System.Buffers.Text
{
    // All the helper methods in this class assume that the by-ref is valid and that there is
    // enough space to fit the items that will be written into the underlying memory. The calling
    // code must have already done all the necessary validation.
    internal static class FormattingHelpers
    {
        // For the purpose of formatting time, the format specifier contains room for
        // exactly 7 digits in the fraction portion. See "Round-trip format specifier"
        // at the following URL for more information.
        // https://msdn.microsoft.com/en-us/library/az4se3k1(v=vs.110).aspx#Roundtrip
        private const int FractionDigits = 7;

        // A simple lookup table for converting numbers to hex.
        private const string HexTable = "0123456789abcdef";

        #region UTF-8 Helper methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteHexByte(byte value, ref byte buffer, int index)
        {
            Unsafe.Add(ref buffer, index) = (byte)HexTable[value >> 4];
            Unsafe.Add(ref buffer, index + 1) = (byte)HexTable[value & 0xF];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFractionDigits(long value, int digitCount, ref byte buffer, int index)
        {
            for (int i = FractionDigits; i > digitCount; i--)
                value /= 10;

            WriteDigits(value, digitCount, ref buffer, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDigits(ulong value, Span<byte> buffer)
        {
            ulong left = value;

            for (int i = buffer.Length - 1; i >= 1; i--)
            {
                left = DivMod(left, 10, out ulong num);
                buffer[i] = (byte)('0' + num);
            }

            Debug.Assert(left < 10);
            buffer[0] = (byte)('0' + left);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDigits(uint value, Span<byte> buffer)
        {
            uint left = value;

            for (int i = buffer.Length - 1; i >= 1; i--)
            {
                left = DivMod(left, 10, out uint num);
                buffer[i] = (byte)('0' + num);
            }

            Debug.Assert(left < 10);
            buffer[0] = (byte)('0' + left);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDigits(long value, int digitCount, ref byte buffer, int index)
        {
            long left = value;

            for (int i = digitCount - 1; i >= 0; i--)
            {
                left = DivMod(left, 10, out long num);
                Unsafe.Add(ref buffer, index + i) = (byte)('0' + num);
            }

            Debug.Assert(left == 0);
        }

        /// <summary>
        /// The unsigned long implementation of this method is much slower than the signed version above
        /// due to optimization tricks that happen at the IL to ASM stage. Use the signed version unless
        /// you definitely need to deal with numbers larger than long.MaxValue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDigits(ulong value, int digitCount, ref byte buffer, int index)
        {
            ulong left = value;

            for (int i = digitCount - 1; i >= 0; i--)
            {
                left = DivMod(left, 10, out ulong num);
                Unsafe.Add(ref buffer, index + i) = (byte)('0' + num);
            }

            Debug.Assert(left == 0);
        }

        #endregion UTF-8 Helper methods

        #region Math Helper methods

        /// <summary>
        /// We don't have access to Math.DivRem, so this is a copy of the implementation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long DivMod(long numerator, long denominator, out long modulo)
        {
            long div = numerator / denominator;
            modulo = numerator - (div * denominator);
            return div;
        }

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

        #region Character counting helper methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountDigits(long n)
        {
            if (n == 0)
                return 1;

            int digits = 0;
            while (n != 0)
            {
                n /= 10;
                digits++;
            }

            return digits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountDigits(ulong value)
        {
            int digits = 1;
            uint part;
            if (value >= 10000000)
            {
                if (value >= 100000000000000)
                {
                    part = (uint)(value / 100000000000000);
                    digits += 14;
                }
                else
                {
                    part = (uint)(value / 10000000);
                    digits += 7;
                }
            }
            else
            {
                part = (uint)value;
            }

            if (part < 10)
            {
                // no-op
            }
            else if (part < 100)
            {
                digits += 1;
            }
            else if (part < 1000)
            {
                digits += 2;
            }
            else if (part < 10000)
            {
                digits += 3;
            }
            else if (part < 100000)
            {
                digits += 4;
            }
            else if (part < 1000000)
            {
                digits += 5;
            }
            else
            {
                Debug.Assert(part < 10000000);
                digits += 6;
            }

            return digits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountDigits(uint value)
        {
            int digits = 1;
            if (value >= 100000)
            {
                value = value / 100000;
                digits += 5;
            }

            if (value < 10) 
            { 
                // no-op
            }
            else if (value < 100) 
            {
                digits += 1;
            }
            else if (value < 1000)
            {
                digits += 2;
            }
            else if (value < 10000)
            {
                digits += 3;
            }
            else
            {
                Debug.Assert(value < 100000);
                digits += 4;
            }

            return digits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountFractionDigits(long n)
        {
            Debug.Assert(n >= 0);

            long left = n;
            long m = 0;
            int count = FractionDigits;

            // Remove all the 0 (zero) values from the right.
            while (left > 0 && m == 0 && count > 0)
            {
                left = DivMod(left, 10, out m);
                count--;
            }

            return count + 1;
        }

        #endregion Character counting helper methods
    }
}
