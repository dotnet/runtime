// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics
{
    internal static class NumericsHelpers
    {
        public static void GetDoubleParts(double dbl, out int sign, out int exp, out ulong man, out bool fFinite)
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(dbl);

            sign = 1 - ((int)(bits >> 62) & 2);
            man = bits & 0x000FFFFFFFFFFFFF;
            exp = (int)(bits >> 52) & 0x7FF;
            if (exp == 0)
            {
                // Denormalized number.
                fFinite = true;
                if (man != 0)
                {
                    exp = -1074;
                }
            }
            else if (exp == 0x7FF)
            {
                // NaN or Infinite.
                fFinite = false;
                exp = int.MaxValue;
            }
            else
            {
                fFinite = true;
                man |= 0x0010000000000000;
                exp -= 1075;
            }
        }

        public static double GetDoubleFromParts(int sign, int exp, ulong man)
        {
            ulong bits;

            if (man == 0)
            {
                bits = 0;
            }
            else
            {
                // Normalize so that 0x0010 0000 0000 0000 is the highest bit set.
                int cbitShift = BitOperations.LeadingZeroCount(man) - 11;
                if (cbitShift < 0)
                {
                    man >>= -cbitShift;
                }
                else
                {
                    man <<= cbitShift;
                }

                exp -= cbitShift;
                Debug.Assert((man & 0xFFF0000000000000) == 0x0010000000000000);

                // Move the point to just behind the leading 1: 0x001.0 0000 0000 0000
                // (52 bits) and skew the exponent (by 0x3FF == 1023).
                exp += 1075;

                if (exp >= 0x7FF)
                {
                    // Infinity.
                    bits = 0x7FF0000000000000;
                }
                else if (exp <= 0)
                {
                    // Denormalized.
                    exp--;
                    if (exp < -52)
                    {
                        // Underflow to zero.
                        bits = 0;
                    }
                    else
                    {
                        bits = man >> -exp;
                        Debug.Assert(bits != 0);
                    }
                }
                else
                {
                    // Mask off the implicit high bit.
                    bits = (man & 0x000FFFFFFFFFFFFF) | ((ulong)exp << 52);
                }
            }

            if (sign < 0)
            {
                bits |= 0x8000000000000000;
            }

            return BitConverter.UInt64BitsToDouble(bits);
        }

        /// <summary>Performs an in-place two's complement. Use with care for immutable types.</summary>
        public static void DangerousMakeTwosComplement(Span<nuint> d)
        {
            // Given a number:
            //     XXXXXXXXXXXY00000
            // where Y is non-zero,
            // The result of two's complement is
            //     AAAAAAAAAAAB00000
            // where A = ~X and B = -Y

            // Trim trailing 0s (at the first in little endian array)
            int i = d.IndexOfAnyExcept(0u);

            if ((uint)i >= (uint)d.Length)
            {
                return;
            }

            // Make the first non-zero element to be two's complement
            d[i] = (nuint)(-(nint)d[i]);
            d = d.Slice(i + 1);

            if (d.IsEmpty)
            {
                return;
            }

            DangerousMakeOnesComplement(d);
        }

        /// <summary>Performs an in-place one's complement. Use with care for immutable types.</summary>
        public static void DangerousMakeOnesComplement(Span<nuint> d)
        {
            // Given a number:
            //     XXXXXXXXXXX
            // where Y is non-zero,
            // The result of one's complement is
            //     AAAAAAAAAAA
            // where A = ~X

            while (Vector512.IsHardwareAccelerated && d.Length >= Vector512<nuint>.Count)
            {
                Vector512<nuint> complement = ~Vector512.Create(d);
                complement.CopyTo(d);
                d = d.Slice(Vector512<nuint>.Count);
            }

            while (Vector256.IsHardwareAccelerated && d.Length >= Vector256<nuint>.Count)
            {
                Vector256<nuint> complement = ~Vector256.Create(d);
                complement.CopyTo(d);
                d = d.Slice(Vector256<nuint>.Count);
            }

            while (Vector128.IsHardwareAccelerated && d.Length >= Vector128<nuint>.Count)
            {
                Vector128<nuint> complement = ~Vector128.Create(d);
                complement.CopyTo(d);
                d = d.Slice(Vector128<nuint>.Count);
            }

            for (int i = 0; i < d.Length; i++)
            {
                d[i] = ~d[i];
            }
        }

        /// <summary>Branchless abs: arithmetic right shift produces 0 (positive) or -1 (negative) mask,
        /// then XOR-and-subtract flips negative values without branching.</summary>
        public static nuint Abs(int a)
        {
            nuint mask = (nuint)(a >> 31);
            return ((nuint)a ^ mask) - mask;
        }
    }
}
