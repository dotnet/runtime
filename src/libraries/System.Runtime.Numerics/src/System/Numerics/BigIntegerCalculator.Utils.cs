// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using X86Base = System.Runtime.Intrinsics.X86.X86Base;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        /// <summary>Number of bits per native-width limb: 32 on 32-bit, 64 on 64-bit.</summary>
        internal static int BitsPerLimb
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => nint.Size * 8;
        }

        public static int Compare(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right)
        {
            Debug.Assert(left.Length <= right.Length || left.Slice(right.Length).ContainsAnyExcept(0u));
            Debug.Assert(left.Length >= right.Length || right.Slice(left.Length).ContainsAnyExcept(0u));

            if (left.Length != right.Length)
            {
                return left.Length < right.Length ? -1 : 1;
            }

            int iv = left.Length;
            while (--iv >= 0 && left[iv] == right[iv]) ;

            if (iv < 0)
            {
                return 0;
            }

            return left[iv] < right[iv] ? -1 : 1;
        }

        private static int CompareActual(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right)
        {
            if (left.Length != right.Length)
            {
                if (left.Length < right.Length)
                {
                    if (ActualLength(right.Slice(left.Length)) > 0)
                    {
                        return -1;
                    }

                    right = right.Slice(0, left.Length);
                }
                else
                {
                    if (ActualLength(left.Slice(right.Length)) > 0)
                    {
                        return +1;
                    }

                    left = left.Slice(0, right.Length);
                }
            }

            return Compare(left, right);
        }

        public static int ActualLength(ReadOnlySpan<nuint> value)
        {
            // Since we're reusing memory here, the actual length
            // of a given value may be less then the array's length

            int length = value.Length;

            while (length > 0 && value[length - 1] == 0)
            {
                --length;
            }

            return length;
        }

        private static int Reduce(Span<nuint> bits, ReadOnlySpan<nuint> modulus)
        {
            // Executes a modulo operation using the divide operation.

            if (bits.Length >= modulus.Length)
            {
                DivRem(bits, modulus, default);

                return ActualLength(bits.Slice(0, modulus.Length));
            }

            return bits.Length;
        }

        [Conditional("DEBUG")]
        public static void InitializeForDebug(Span<nuint> bits)
        {
            // Reproduce the case where the return value of `stackalloc nuint` is not initialized to zero.
            bits.Fill(0xCD);
        }

        /// <summary>
        /// Performs widening addition of two limbs plus a carry-in, returning the sum and carry-out.
        /// On 64-bit: uses 128-bit arithmetic. On 32-bit: uses 64-bit arithmetic.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nuint AddWithCarry(nuint a, nuint b, nuint carryIn, out nuint carryOut)
        {
            if (nint.Size == 8)
            {
                nuint sum1 = a + b;
                nuint c1 = (sum1 < a) ? 1 : (nuint)0;
                nuint sum2 = sum1 + carryIn;
                nuint c2 = (sum2 < sum1) ? 1 : (nuint)0;
                carryOut = c1 + c2;
                return sum2;
            }
            else
            {
                ulong sum = (ulong)a + b + carryIn;
                carryOut = (uint)(sum >> 32);
                return (uint)sum;
            }
        }

        /// <summary>
        /// Performs widening subtraction of two limbs with a borrow-in, returning the difference and borrow-out.
        /// borrowOut is 0 (no borrow) or 1 (borrow occurred).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nuint SubWithBorrow(nuint a, nuint b, nuint borrowIn, out nuint borrowOut)
        {
            if (nint.Size == 8)
            {
                // Use unsigned underflow detection
                nuint diff1 = a - b;
                nuint b1 = (diff1 > a) ? 1 : (nuint)0;
                nuint diff2 = diff1 - borrowIn;
                nuint b2 = (diff2 > diff1) ? 1 : (nuint)0;
                borrowOut = b1 + b2;
                return diff2;
            }
            else
            {
                long diff = (long)a - (long)b - (long)borrowIn;
                borrowOut = (uint)(-(int)(diff >> 32)); // 0 or 1
                return (uint)diff;
            }
        }

        /// <summary>
        /// Widening divide: (hi:lo) / divisor -> (quotient, remainder).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nuint DivRem(nuint hi, nuint lo, nuint divisor, out nuint remainder)
        {
            if (nint.Size == 8)
            {
                // Compute (hi * 2^64 + lo) / divisor.
                // hi < divisor is guaranteed by callers, so quotient fits in 64 bits.
                Debug.Assert(hi < (ulong)divisor || divisor == 0);

                if (hi == 0)
                {
                    (ulong q, ulong r) = Math.DivRem(lo, (ulong)divisor);
                    remainder = (nuint)r;
                    return (nuint)q;
                }

                // When divisor fits in 32 bits, split lo into two 32-bit halves
                // and chain two native 64-bit divisions (avoids UInt128 overhead):
                //   (hi * 2^32 + lo_hi) / divisor -> (q_hi, r1) [fits: hi < divisor < 2^32]
                //   (r1 * 2^32 + lo_lo) / divisor -> (q_lo, r2) [fits: r1 < divisor < 2^32]
                if ((ulong)divisor <= uint.MaxValue)
                {
                    ulong lo_hi = (ulong)lo >> 32;
                    ulong lo_lo = (ulong)lo & 0xFFFFFFFF;

                    (ulong q_hi, ulong r1) = Math.DivRem(((ulong)hi << 32) | lo_hi, divisor);
                    (ulong q_lo, ulong r2) = Math.DivRem((r1 << 32) | lo_lo, divisor);

                    remainder = (nuint)r2;
                    return (nuint)((q_hi << 32) | q_lo);
                }

                {
#pragma warning disable SYSLIB5004 // X86Base.DivRem is experimental
                    if (X86Base.X64.IsSupported)
                    {
                        (ulong q, ulong r) = X86Base.X64.DivRem(lo, hi, divisor);
                        remainder = (nuint)r;
                        return (nuint)q;
                    }
#pragma warning restore SYSLIB5004

                    UInt128 value = ((UInt128)(ulong)hi << 64) | (ulong)lo;
                    UInt128 digit = value / (ulong)divisor;
                    remainder = (nuint)(ulong)(value - digit * (ulong)divisor);
                    return (nuint)(ulong)digit;
                }
            }
            else
            {
                ulong value = ((ulong)hi << 32) | lo;
                ulong digit = value / divisor;
                remainder = (uint)(value - digit * divisor);
                return (uint)digit;
            }
        }

        /// <summary>
        /// Multiply by scalar: result[0..left.Length] = left * multiplier.
        /// Returns the carry out. Unrolled by 4 on 64-bit.
        /// Unlike MulAdd1, this writes to result rather than accumulating.
        /// </summary>
        internal static nuint Mul1(Span<nuint> result, ReadOnlySpan<nuint> left, nuint multiplier)
        {
            Debug.Assert(result.Length >= left.Length);

            int length = left.Length;
            int i = 0;
            nuint carry = 0;

            if (nint.Size == 8)
            {
                for (; i + 3 < length; i += 4)
                {
                    UInt128 p0 = (UInt128)(ulong)left[i] * (ulong)multiplier + (ulong)carry;
                    result[i] = (nuint)(ulong)p0;

                    UInt128 p1 = (UInt128)(ulong)left[i + 1] * (ulong)multiplier + (ulong)(p0 >> 64);
                    result[i + 1] = (nuint)(ulong)p1;

                    UInt128 p2 = (UInt128)(ulong)left[i + 2] * (ulong)multiplier + (ulong)(p1 >> 64);
                    result[i + 2] = (nuint)(ulong)p2;

                    UInt128 p3 = (UInt128)(ulong)left[i + 3] * (ulong)multiplier + (ulong)(p2 >> 64);
                    result[i + 3] = (nuint)(ulong)p3;

                    carry = (nuint)(ulong)(p3 >> 64);
                }

                for (; i < length; i++)
                {
                    UInt128 product = (UInt128)(ulong)left[i] * (ulong)multiplier + (ulong)carry;
                    result[i] = (nuint)(ulong)product;
                    carry = (nuint)(ulong)(product >> 64);
                }
            }
            else
            {
                for (; i < length; i++)
                {
                    ulong product = (ulong)left[i] * multiplier + carry;
                    result[i] = (uint)product;
                    carry = (uint)(product >> 32);
                }
            }

            return carry;
        }

        /// <summary>
        /// Fused multiply-accumulate by scalar: result[0..left.Length] += left * multiplier.
        /// Returns the carry out. Unrolled by 4 on 64-bit to overlap multiply latencies.
        /// </summary>
        internal static nuint MulAdd1(Span<nuint> result, ReadOnlySpan<nuint> left, nuint multiplier)
        {
            Debug.Assert(result.Length >= left.Length);

            int length = left.Length;
            int i = 0;
            nuint carry = 0;

            if (nint.Size == 8)
            {
                // Unroll by 4: mulx has 3-5 cycle latency but 1 cycle throughput,
                // so issuing 4 multiplies allows the CPU to pipeline them while
                // carry chains complete sequentially behind.
                for (; i + 3 < length; i += 4)
                {
                    UInt128 p0 = (UInt128)(ulong)left[i] * (ulong)multiplier + (ulong)result[i] + (ulong)carry;
                    result[i] = (nuint)(ulong)p0;

                    UInt128 p1 = (UInt128)(ulong)left[i + 1] * (ulong)multiplier + (ulong)result[i + 1] + (ulong)(p0 >> 64);
                    result[i + 1] = (nuint)(ulong)p1;

                    UInt128 p2 = (UInt128)(ulong)left[i + 2] * (ulong)multiplier + (ulong)result[i + 2] + (ulong)(p1 >> 64);
                    result[i + 2] = (nuint)(ulong)p2;

                    UInt128 p3 = (UInt128)(ulong)left[i + 3] * (ulong)multiplier + (ulong)result[i + 3] + (ulong)(p2 >> 64);
                    result[i + 3] = (nuint)(ulong)p3;

                    carry = (nuint)(ulong)(p3 >> 64);
                }

                for (; i < length; i++)
                {
                    UInt128 product = (UInt128)(ulong)left[i] * (ulong)multiplier + (ulong)result[i] + (ulong)carry;
                    result[i] = (nuint)(ulong)product;
                    carry = (nuint)(ulong)(product >> 64);
                }
            }
            else
            {
                for (; i < length; i++)
                {
                    ulong product = (ulong)left[i] * multiplier
                                    + result[i] + carry;
                    result[i] = (uint)product;
                    carry = (uint)(product >> 32);
                }
            }

            return carry;
        }

        /// <summary>
        /// Fused subtract-multiply by scalar: result[0..right.Length] -= right * multiplier.
        /// Returns the borrow out. Unrolled by 4 on 64-bit.
        /// </summary>
        internal static nuint SubMul1(Span<nuint> result, ReadOnlySpan<nuint> right, nuint multiplier)
        {
            Debug.Assert(result.Length >= right.Length);

            int length = right.Length;
            int i = 0;
            nuint carry = 0;

            if (nint.Size == 8)
            {
                for (; i + 3 < length; i += 4)
                {
                    UInt128 prod0 = (UInt128)(ulong)right[i] * (ulong)multiplier + (ulong)carry;
                    nuint lo0 = (nuint)(ulong)prod0;
                    nuint hi0 = (nuint)(ulong)(prod0 >> 64);
                    nuint orig0 = result[i];
                    result[i] = orig0 - lo0;
                    hi0 += (orig0 < lo0) ? (nuint)1 : 0;

                    UInt128 prod1 = (UInt128)(ulong)right[i + 1] * (ulong)multiplier + (ulong)hi0;
                    nuint lo1 = (nuint)(ulong)prod1;
                    nuint hi1 = (nuint)(ulong)(prod1 >> 64);
                    nuint orig1 = result[i + 1];
                    result[i + 1] = orig1 - lo1;
                    hi1 += (orig1 < lo1) ? (nuint)1 : 0;

                    UInt128 prod2 = (UInt128)(ulong)right[i + 2] * (ulong)multiplier + (ulong)hi1;
                    nuint lo2 = (nuint)(ulong)prod2;
                    nuint hi2 = (nuint)(ulong)(prod2 >> 64);
                    nuint orig2 = result[i + 2];
                    result[i + 2] = orig2 - lo2;
                    hi2 += (orig2 < lo2) ? (nuint)1 : 0;

                    UInt128 prod3 = (UInt128)(ulong)right[i + 3] * (ulong)multiplier + (ulong)hi2;
                    nuint lo3 = (nuint)(ulong)prod3;
                    nuint hi3 = (nuint)(ulong)(prod3 >> 64);
                    nuint orig3 = result[i + 3];
                    result[i + 3] = orig3 - lo3;
                    hi3 += (orig3 < lo3) ? (nuint)1 : 0;

                    carry = hi3;
                }

                for (; i < length; i++)
                {
                    UInt128 product = (UInt128)(ulong)right[i] * (ulong)multiplier + (ulong)carry;
                    nuint lo = (nuint)(ulong)product;
                    nuint hi = (nuint)(ulong)(product >> 64);
                    nuint orig = result[i];
                    result[i] = orig - lo;
                    hi += (orig < lo) ? (nuint)1 : 0;
                    carry = hi;
                }
            }
            else
            {
                for (; i < length; i++)
                {
                    ulong product = (ulong)right[i] * multiplier + carry;
                    uint lo = (uint)product;
                    uint hi = (uint)(product >> 32);

                    uint orig = (uint)result[i];
                    result[i] = orig - lo;
                    hi += (orig < lo) ? 1u : 0;

                    carry = hi;
                }
            }

            return carry;
        }
    }
}
