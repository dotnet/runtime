// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics
{
    // Magnitude (unsigned, native-limb) arithmetic shared between the public
    // System.Numerics.BigInteger and the internal System.Number.BigInteger used by
    // floating-point parsing/formatting/rounding. Both are nuint-backed, so these kernels
    // operate purely on Span<nuint>/ReadOnlySpan<nuint> and are free of any sign, allocation,
    // or type-specific policy. This file is compiled into System.Private.CoreLib and linked
    // into System.Runtime.Numerics.
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
            // of a given value may be less than the array's length

            return value.LastIndexOfAnyExcept((nuint)0) + 1;
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

        private const int CopyToThreshold = 8;

        private static void CopyTail(ReadOnlySpan<nuint> source, Span<nuint> dest, int start)
        {
            source.Slice(start).CopyTo(dest.Slice(start));
        }

        public static void Add(ReadOnlySpan<nuint> left, nuint right, Span<nuint> bits)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(bits.Length == left.Length + 1);

            Add(left, bits, startIndex: 0, initialCarry: right);
        }

        public static void Add(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits)
        {
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(bits.Length == left.Length + 1);

            // Establish cross-span length relationships so the JIT can
            // elide bounds checks for left[i] and bits[i] in the loop.
            _ = left[right.Length - 1];
            _ = bits[right.Length];

            nuint carry = 0;

            for (int i = 0; i < right.Length; i++)
            {
                bits[i] = AddWithCarry(left[i], right[i], carry, out carry);
            }

            Add(left, bits, startIndex: right.Length, initialCarry: carry);
        }

        public static void AddSelf(Span<nuint> left, ReadOnlySpan<nuint> right)
        {
            Debug.Assert(left.Length >= right.Length);

            int i = 0;
            nuint carry = 0;

            if (right.Length != 0)
            {
                _ = left[right.Length - 1];
            }

            for (; i < right.Length; i++)
            {
                left[i] = AddWithCarry(left[i], right[i], carry, out carry);
            }

            for (; carry != 0 && i < left.Length; i++)
            {
                nuint sum = left[i] + carry;
                carry = (sum < carry) ? 1 : (nuint)0;
                left[i] = sum;
            }

            Debug.Assert(carry == 0);
        }

        public static void Subtract(ReadOnlySpan<nuint> left, nuint right, Span<nuint> bits)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(left[0] >= right || left.Length >= 2);
            Debug.Assert(bits.Length == left.Length);

            Subtract(left, bits, startIndex: 0, initialBorrow: right);
        }

        public static void Subtract(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits)
        {
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(CompareActual(left, right) >= 0);
            Debug.Assert(bits.Length == left.Length);

            _ = left[right.Length - 1];
            _ = bits[right.Length - 1];

            nuint borrow = 0;

            for (int i = 0; i < right.Length; i++)
            {
                bits[i] = SubWithBorrow(left[i], right[i], borrow, out borrow);
            }

            Subtract(left, bits, startIndex: right.Length, initialBorrow: borrow);
        }

        public static void SubtractSelf(Span<nuint> left, ReadOnlySpan<nuint> right)
        {
            Debug.Assert(left.Length >= right.Length);

            int i = 0;
            nuint borrow = 0;

            if (right.Length != 0)
            {
                _ = left[right.Length - 1];
            }

            for (; i < right.Length; i++)
            {
                left[i] = SubWithBorrow(left[i], right[i], borrow, out borrow);
            }

            for (; borrow != 0 && i < left.Length; i++)
            {
                nuint val = left[i];
                left[i] = val - borrow;
                borrow = val == 0 ? 1 : (nuint)0;
            }

            Debug.Assert(borrow == 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Add(ReadOnlySpan<nuint> left, Span<nuint> bits, int startIndex, nuint initialCarry)
        {
            // Executes the addition for one big and one single-limb integer.

            int i = startIndex;
            nuint carry = initialCarry;

            _ = bits[left.Length];

            if (left.Length <= CopyToThreshold)
            {
                for (; i < left.Length; i++)
                {
                    nuint sum = left[i] + carry;
                    carry = (sum < carry) ? 1 : (nuint)0;
                    bits[i] = sum;
                }

                bits[left.Length] = carry;
            }
            else
            {
                for (; i < left.Length;)
                {
                    nuint sum = left[i] + carry;
                    carry = (sum < carry) ? 1 : (nuint)0;
                    bits[i] = sum;
                    i++;

                    // Once carry is set to 0 it can not be 1 anymore.
                    // So the tail of the loop is just the movement of argument values to result span.
                    if (carry == 0)
                    {
                        break;
                    }
                }

                bits[left.Length] = carry;

                if (i < left.Length)
                {
                    CopyTail(left, bits, i);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Subtract(ReadOnlySpan<nuint> left, Span<nuint> bits, int startIndex, nuint initialBorrow)
        {
            // Executes the subtraction for one big and one single-limb integer.

            int i = startIndex;
            nuint borrow = initialBorrow;

            if (left.Length != 0)
            {
                _ = bits[left.Length - 1];
            }

            if (left.Length <= CopyToThreshold)
            {
                for (; i < left.Length; i++)
                {
                    nuint val = left[i];
                    nuint diff = val - borrow;
                    borrow = (diff > val) ? 1 : (nuint)0;
                    bits[i] = diff;
                }
            }
            else
            {
                for (; i < left.Length;)
                {
                    nuint val = left[i];
                    nuint diff = val - borrow;
                    borrow = (diff > val) ? 1 : (nuint)0;
                    bits[i] = diff;
                    i++;

                    // Once borrow is set to 0 it can not be 1 anymore.
                    // So the tail of the loop is just the movement of argument values to result span.
                    if (borrow == 0)
                    {
                        break;
                    }
                }

                if (i < left.Length)
                {
                    CopyTail(left, bits, i);
                }
            }
        }

        /// <summary>
        /// Multiply using the "grammar-school" method: bits += left * right[i] per limb of right.
        /// Callers guarantee left.Length >= right.Length and bits large enough for the product.
        /// </summary>
        public static void MultiplyNaive(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits)
        {
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(bits.Length >= left.Length + right.Length);

            // Multiplies the bits using the "grammar-school" method.
            // Envisioning the "rhombus" of a pen-and-paper calculation
            // should help getting the idea of these two loops...
            // The inner multiplication operations are safe, because
            // z_i+j + a_j * b_i + c <= 2(2^n - 1) + (2^n - 1)^2 =
            // = 2^(2n) - 1, where n = BitsPerLimb.

            for (int i = 0; i < right.Length; i++)
            {
                nuint carry = MulAdd1(bits.Slice(i), left, right[i]);
                bits[i + left.Length] = carry;
            }
        }

        internal static void DivideGrammarSchool(Span<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> quotient)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(
                quotient.Length == 0
                || quotient.Length == left.Length - right.Length + 1
                || (CompareActual(left.Slice(left.Length - right.Length), right) < 0 && quotient.Length == left.Length - right.Length));

            // Executes the "grammar-school" algorithm for computing q = a / b.
            // Before calculating q_i, we get more bits into the highest bit
            // block of the divisor. Thus, guessing digits of the quotient
            // will be more precise. Additionally we'll get r = a % b.

            nuint divHi = right[^1];
            nuint divLo = right.Length > 1 ? right[^2] : 0;

            // We measure the leading zeros of the divisor
            int shift = (int)nuint.LeadingZeroCount(divHi);
            int backShift = BitsPerLimb - shift;

            // And, we make sure the most significant bit is set
            if (shift > 0)
            {
                nuint divNx = right.Length > 2 ? right[^3] : 0;

                divHi = (divHi << shift) | (divLo >> backShift);
                divLo = (divLo << shift) | (divNx >> backShift);
            }

            // Then, we divide all of the bits as we would do it using
            // pen and paper: guessing the next digit, subtracting, ...
            for (int i = left.Length; i >= right.Length; i--)
            {
                int n = i - right.Length;
                nuint t = (uint)i < (uint)left.Length ? left[i] : 0;

                nuint valHi1 = t;
                nuint valHi0 = left[i - 1];
                nuint valLo = i > 1 ? left[i - 2] : 0;

                // We shifted the divisor, we shift the dividend too
                if (shift > 0)
                {
                    nuint valNx = i > 2 ? left[i - 3] : 0;

                    valHi1 = (valHi1 << shift) | (valHi0 >> backShift);
                    valHi0 = (valHi0 << shift) | (valLo >> backShift);
                    valLo = (valLo << shift) | (valNx >> backShift);
                }

                // First guess for the current digit of the quotient,
                // which naturally must have only native-width bits...
                nuint digit = (valHi1 >= divHi) ? nuint.MaxValue : DivRem(valHi1, valHi0, divHi, out _);

                // Our first guess may be a little bit too big
                while (DivideGuessTooBig(digit, valHi1, valHi0, valLo, divHi, divLo))
                {
                    --digit;
                }

                if (digit > 0)
                {
                    // Now it's time to subtract our current quotient
                    nuint carry = SubtractDivisor(left.Slice(n), right, digit);
                    if (carry != t)
                    {
                        Debug.Assert(carry == t + 1);

                        // Our guess was still exactly one too high
                        carry = AddDivisor(left.Slice(n), right);
                        --digit;

                        Debug.Assert(carry == 1);
                    }
                }

                // We have the digit!
                if ((uint)n < (uint)quotient.Length)
                {
                    quotient[n] = digit;
                }

                if ((uint)i < (uint)left.Length)
                {
                    left[i] = 0;
                }
            }
        }

        private static nuint AddDivisor(Span<nuint> left, ReadOnlySpan<nuint> right)
        {
            Debug.Assert(left.Length >= right.Length);

            // Repairs the dividend, if the last subtract was too much

            nuint carry = 0;

            for (int i = 0; i < right.Length; i++)
            {
                ref nuint leftElement = ref left[i];
                leftElement = AddWithCarry(leftElement, right[i], carry, out carry);
            }

            return carry;
        }

        private static nuint SubtractDivisor(Span<nuint> left, ReadOnlySpan<nuint> right, nuint q)
        {
            Debug.Assert(left.Length >= right.Length);

            return SubMul1(left, right, q);
        }

        private static bool DivideGuessTooBig(nuint q, nuint valHi1, nuint valHi0,
                                              nuint valLo, nuint divHi, nuint divLo)
        {
            // We multiply the two most significant limbs of the divisor
            // with the current guess for the quotient. If those are bigger
            // than the three most significant limbs of the current dividend
            // we return true, which means the current guess is still too big.

            nuint chkHiHi = nuint.BigMul(divHi, q, out nuint chkHiLo);
            nuint chkLoHi = nuint.BigMul(divLo, q, out nuint chkLoLo);

            chkHiLo += chkLoHi;
            if (chkHiLo < chkLoHi)
            {
                chkHiHi++;
            }

            return (chkHiHi > valHi1)
                || ((chkHiHi == valHi1) && ((chkHiLo > valHi0) || ((chkHiLo == valHi0) && (chkLoLo > valLo))));
        }
    }
}
