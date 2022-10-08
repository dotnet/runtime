// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers.Text
{
    internal static partial class FormattingHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountDigits(UInt128 value)
        {
            ulong upper = value.Upper;

            // 1e19 is    8AC7_2304_89E8_0000
            // 1e20 is  5_6BC7_5E2D_6310_0000
            // 1e21 is 36_35C9_ADC5_DEA0_0000

            if (upper == 0)
            {
                // We have less than 64-bits, so just return the lower count
                return CountDigits(value.Lower);
            }

            // We have more than 1e19, so we have at least 20 digits
            int digits = 20;

            if (upper > 5)
            {
                // ((2^128) - 1) / 1e20 < 34_02_823_669_209_384_635 which
                // is 18.5318 digits, meaning the result definitely fits
                // into 64-bits and we only need to add the lower digit count

                value /= new UInt128(0x5, 0x6BC7_5E2D_6310_0000); // value /= 1e20
                Debug.Assert(value.Upper == 0);

                digits += CountDigits(value.Lower);
            }
            else if ((upper == 5) && (value.Lower >= 0x6BC75E2D63100000))
            {
                // We're greater than 1e20, but definitely less than 1e21
                // so we have exactly 21 digits

                digits++;
                Debug.Assert(digits == 21);
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
                digits++;
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
            // Algorithm based on https://lemire.me/blog/2021/06/03/computing-the-number-of-digits-of-an-integer-even-faster.
            // TODO https://github.com/dotnet/runtime/issues/60948: Use ReadOnlySpan<long> instead of ReadOnlySpan<byte>.
            ReadOnlySpan<byte> table = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, // 4294967296
                0xF6, 0xFF, 0xFF, 0xFF, 0x01, 0x00, 0x00, 0x00, // 8589934582
                0xF6, 0xFF, 0xFF, 0xFF, 0x01, 0x00, 0x00, 0x00, // 8589934582
                0xF6, 0xFF, 0xFF, 0xFF, 0x01, 0x00, 0x00, 0x00, // 8589934582
                0x9C, 0xFF, 0xFF, 0xFF, 0x02, 0x00, 0x00, 0x00, // 12884901788
                0x9C, 0xFF, 0xFF, 0xFF, 0x02, 0x00, 0x00, 0x00, // 12884901788
                0x9C, 0xFF, 0xFF, 0xFF, 0x02, 0x00, 0x00, 0x00, // 12884901788
                0x18, 0xFC, 0xFF, 0xFF, 0x03, 0x00, 0x00, 0x00, // 17179868184
                0x18, 0xFC, 0xFF, 0xFF, 0x03, 0x00, 0x00, 0x00, // 17179868184
                0x18, 0xFC, 0xFF, 0xFF, 0x03, 0x00, 0x00, 0x00, // 17179868184
                0xF0, 0xD8, 0xFF, 0xFF, 0x04, 0x00, 0x00, 0x00, // 21474826480
                0xF0, 0xD8, 0xFF, 0xFF, 0x04, 0x00, 0x00, 0x00, // 21474826480
                0xF0, 0xD8, 0xFF, 0xFF, 0x04, 0x00, 0x00, 0x00, // 21474826480
                0xF0, 0xD8, 0xFF, 0xFF, 0x04, 0x00, 0x00, 0x00, // 21474826480
                0x60, 0x79, 0xFE, 0xFF, 0x05, 0x00, 0x00, 0x00, // 25769703776
                0x60, 0x79, 0xFE, 0xFF, 0x05, 0x00, 0x00, 0x00, // 25769703776
                0x60, 0x79, 0xFE, 0xFF, 0x05, 0x00, 0x00, 0x00, // 25769703776
                0xC0, 0xBD, 0xF0, 0xFF, 0x06, 0x00, 0x00, 0x00, // 30063771072
                0xC0, 0xBD, 0xF0, 0xFF, 0x06, 0x00, 0x00, 0x00, // 30063771072
                0xC0, 0xBD, 0xF0, 0xFF, 0x06, 0x00, 0x00, 0x00, // 30063771072
                0x80, 0x69, 0x67, 0xFF, 0x07, 0x00, 0x00, 0x00, // 34349738368
                0x80, 0x69, 0x67, 0xFF, 0x07, 0x00, 0x00, 0x00, // 34349738368
                0x80, 0x69, 0x67, 0xFF, 0x07, 0x00, 0x00, 0x00, // 34349738368
                0x80, 0x69, 0x67, 0xFF, 0x07, 0x00, 0x00, 0x00, // 34349738368
                0x00, 0x1F, 0x0A, 0xFA, 0x08, 0x00, 0x00, 0x00, // 38554705664
                0x00, 0x1F, 0x0A, 0xFA, 0x08, 0x00, 0x00, 0x00, // 38554705664
                0x00, 0x1F, 0x0A, 0xFA, 0x08, 0x00, 0x00, 0x00, // 38554705664
                0x00, 0x36, 0x65, 0xC4, 0x09, 0x00, 0x00, 0x00, // 41949672960
                0x00, 0x36, 0x65, 0xC4, 0x09, 0x00, 0x00, 0x00, // 41949672960
                0x00, 0x36, 0x65, 0xC4, 0x09, 0x00, 0x00, 0x00, // 41949672960
                0x00, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x00, // 42949672960
                0x00, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x00, // 42949672960
            };
            Debug.Assert(table.Length == (32 * sizeof(long)), "Every result of uint.Log2(value) needs a long entry in the table.");

            long tableValue = Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref MemoryMarshal.GetReference(table), uint.Log2(value) * sizeof(long)));
            if (!BitConverter.IsLittleEndian)
            {
                tableValue = BinaryPrimitives.ReverseEndianness(tableValue);
            }

            return (int)((value + tableValue) >> 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountHexDigits(UInt128 value)
        {
            // The number of hex digits is log16(value) + 1, or log2(value) / 4 + 1
            return ((int)UInt128.Log2(value) >> 2) + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountHexDigits(ulong value)
        {
            // The number of hex digits is log16(value) + 1, or log2(value) / 4 + 1
            return (BitOperations.Log2(value) >> 2) + 1;
        }

        // Counts the number of trailing '0' digits in a decimal number.
        // e.g., value =      0 => retVal = 0, valueWithoutTrailingZeros = 0
        //       value =   1234 => retVal = 0, valueWithoutTrailingZeros = 1234
        //       value = 320900 => retVal = 2, valueWithoutTrailingZeros = 3209
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountDecimalTrailingZeros(uint value, out uint valueWithoutTrailingZeros)
        {
            int zeroCount = 0;

            if (value != 0)
            {
                while (true)
                {
                    uint temp = value / 10;
                    if (value != (temp * 10))
                    {
                        break;
                    }

                    value = temp;
                    zeroCount++;
                }
            }

            valueWithoutTrailingZeros = value;
            return zeroCount;
        }
    }
}
