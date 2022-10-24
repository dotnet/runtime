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

        // Based on do_count_digits from https://github.com/fmtlib/fmt/blob/662adf4f33346ba9aba8b072194e319869ede54a/include/fmt/format.h#L1124
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountDigits(ulong value)
        {
            // Map the log2(value) to a power of 10.
            ReadOnlySpan<byte> log2ToPow10 = new byte[]
            {
                1,  1,  1,  2,  2,  2,  3,  3,  3,  4,  4,  4,  4,  5,  5,  5,
                6,  6,  6,  7,  7,  7,  7,  8,  8,  8,  9,  9,  9,  10, 10, 10,
                10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 13, 14, 14, 14, 15, 15,
                15, 16, 16, 16, 16, 17, 17, 17, 18, 18, 18, 19, 19, 19, 19, 20
            };
            Debug.Assert(log2ToPow10.Length == 64);
            uint index = Unsafe.Add(ref MemoryMarshal.GetReference(log2ToPow10), BitOperations.Log2(value));

            // TODO https://github.com/dotnet/runtime/issues/60948: Use ReadOnlySpan<ulong> instead of ReadOnlySpan<byte>.
            // Read the associated power of 10.
            ReadOnlySpan<byte> powersOf10 = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // unused entry to avoid needing to subtract
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 0
                0x0A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 10
                0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 100
                0xE8, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 1000
                0x10, 0x27, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 10000
                0xA0, 0x86, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, // 100000
                0x40, 0x42, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, // 1000000
                0x80, 0x96, 0x98, 0x00, 0x00, 0x00, 0x00, 0x00, // 10000000
                0x00, 0xE1, 0xF5, 0x05, 0x00, 0x00, 0x00, 0x00, // 100000000
                0x00, 0xCA, 0x9A, 0x3B, 0x00, 0x00, 0x00, 0x00, // 1000000000
                0x00, 0xE4, 0x0B, 0x54, 0x02, 0x00, 0x00, 0x00, // 10000000000
                0x00, 0xE8, 0x76, 0x48, 0x17, 0x00, 0x00, 0x00, // 100000000000
                0x00, 0x10, 0xA5, 0xD4, 0xE8, 0x00, 0x00, 0x00, // 1000000000000
                0x00, 0xA0, 0x72, 0x4E, 0x18, 0x09, 0x00, 0x00, // 10000000000000
                0x00, 0x40, 0x7A, 0x10, 0xF3, 0x5A, 0x00, 0x00, // 100000000000000
                0x00, 0x80, 0xC6, 0xA4, 0x7E, 0x8D, 0x03, 0x00, // 1000000000000000
                0x00, 0x00, 0xC1, 0x6F, 0xF2, 0x86, 0x23, 0x00, // 10000000000000000
                0x00, 0x00, 0x8A, 0x5D, 0x78, 0x45, 0x63, 0x01, // 100000000000000000
                0x00, 0x00, 0x64, 0xA7, 0xB3, 0xB6, 0xE0, 0x0D, // 1000000000000000000
                0x00, 0x00, 0xE8, 0x89, 0x04, 0x23, 0xC7, 0x8A, // 10000000000000000000
            };
            Debug.Assert((index + 1) * sizeof(ulong) <= powersOf10.Length);
            ulong powerOf10 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref MemoryMarshal.GetReference(powersOf10), index * sizeof(ulong)));
            if (!BitConverter.IsLittleEndian)
            {
                powerOf10 = BinaryPrimitives.ReverseEndianness(powerOf10);
            }

            // Return the number of digits based on the power of 10, shifted by 1
            // if it falls below the threshold.
            bool lessThan = value < powerOf10;
            return (int)(index - Unsafe.As<bool, byte>(ref lessThan)); // while arbitrary bools may be non-0/1, comparison operators are expected to return 0/1
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
