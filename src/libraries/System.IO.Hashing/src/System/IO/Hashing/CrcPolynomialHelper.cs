// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace System.IO.Hashing
{
    /// <summary>
    /// Provides GF(2) polynomial arithmetic for computing CRC folding constants.
    /// </summary>
    internal static class CrcPolynomialHelper
    {
        /// <summary>
        /// Computes x^<paramref name="power"/> mod <paramref name="poly"/> in GF(2).
        /// </summary>
        /// <param name="poly">The polynomial (with leading bit) to reduce by.</param>
        /// <param name="power">The power of x.</param>
        /// <returns>The remainder, which has fewer bits than <paramref name="poly"/>.</returns>
        internal static ulong ComputeFoldingConstant(UInt640 poly, int power)
        {
            int polyDeg = poly.Degree;

            UInt640 value = new(1);
            value.ShiftLeftEquals(power);

            while (value.Degree >= polyDeg)
            {
                int shift = value.Degree - polyDeg;
                UInt640 polyShifted = poly;
                polyShifted.ShiftLeftEquals(shift);
                value.XorEquals(ref polyShifted);
            }

            return value.ToUInt64();
        }

        /// <summary>
        /// Computes floor(x^<paramref name="power"/> / <paramref name="poly"/>) in GF(2).
        /// </summary>
        /// <param name="poly">The polynomial (with leading bit) to divide by.</param>
        /// <param name="power">The power of x.</param>
        /// <returns>The quotient.</returns>
        internal static ulong ComputeBarrettConstant(UInt640 poly, int power)
        {
            int polyDeg = poly.Degree;

            UInt640 value = new(1);
            value.ShiftLeftEquals(power);

            UInt640 quotient = default;

            while (value.Degree >= polyDeg)
            {
                int shift = value.Degree - polyDeg;
                UInt640 polyShifted = poly;
                polyShifted.ShiftLeftEquals(shift);
                value.XorEquals(ref polyShifted);

                UInt640 bit = new(1);
                bit.ShiftLeftEquals(shift);
                quotient.XorEquals(ref bit);
            }

            return quotient.ToUInt64();
        }

        /// <summary>
        /// Reverses the lowest <paramref name="width"/> bits of <paramref name="value"/>.
        /// </summary>
        internal static ulong ReverseBits(ulong value, int width)
        {
            ulong result = 0;

            for (int i = 0; i < width; i++)
            {
                if ((value & (1UL << i)) != 0)
                {
                    result |= 1UL << (width - 1 - i);
                }
            }

            return result;
        }

        /// <summary>
        /// A 640-bit unsigned integer for GF(2) polynomial arithmetic.
        /// </summary>
        [InlineArray(Length)]
        internal struct UInt640
        {
            private const int Length = 10;
            private ulong _element;

            internal UInt640(ulong value)
            {
                this = default;
                this[0] = value;
            }

            internal readonly int Degree
            {
                get
                {
                    for (int i = Length - 1; i >= 0; i--)
                    {
                        if (this[i] != 0)
                        {
                            return (i * 64) + (63 - BitOperations.LeadingZeroCount(this[i]));
                        }
                    }

                    return -1;
                }
            }

            internal void ShiftLeftEquals(int count)
            {
                int wordShift = count >> 6; // count / 64
                int bitShift = count & 63;  // count % 64

                if (wordShift > 0)
                {
                    for (int i = Length - 1; i >= wordShift; i--)
                    {
                        this[i] = this[i - wordShift];
                    }

                    for (int i = wordShift - 1; i >= 0; i--)
                    {
                        this[i] = 0;
                    }
                }

                if (bitShift > 0)
                {
                    for (int i = Length - 1; i > 0; i--)
                    {
                        this[i] = (this[i] << bitShift) | (this[i - 1] >> (64 - bitShift));
                    }

                    this[0] <<= bitShift;
                }
            }

            internal void XorEquals(ref UInt640 other)
            {
                for (int i = 0; i < Length; i++)
                {
                    this[i] ^= other[i];
                }
            }

            internal readonly ulong ToUInt64() => this[0];
        }

        /// <summary>
        /// Polyfill for <see cref="System.Numerics.BitOperations.LeadingZeroCount(ulong)"/>.
        /// </summary>
        private static class BitOperations
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static int LeadingZeroCount(ulong value)
            {
                return System.Numerics.BitOperations.LeadingZeroCount(value);
            }
        }
    }
}

#endif
