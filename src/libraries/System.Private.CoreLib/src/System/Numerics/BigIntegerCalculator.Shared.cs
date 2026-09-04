// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
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
        /// <summary>Maximum length of a shifted single-limb divisor after complete zero limbs are removed.</summary>
        private const int ShiftedDivisorMaxReducedLength = 2;

        /// <summary>Number of bits per native-width limb: 32 on 32-bit, 64 on 64-bit.</summary>
        internal static int BitsPerLimb
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => nint.Size * 8;
        }

        [Conditional("DEBUG")]
        public static void InitializeForDebug(Span<nuint> bits)
        {
            bits.Fill(0xCD);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetLimbOffset(ReadOnlySpan<nuint> value)
        {
            Debug.Assert(!value.IsEmpty);

            int offset = value[0] == 0 ? value.IndexOfAnyExcept((nuint)0) : 0;
            Debug.Assert(offset >= 0);
            return offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetCommonLimbOffset(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right)
        {
            return !left.IsEmpty && !right.IsEmpty && left[0] == 0 && right[0] == 0
                ? Math.Min(GetLimbOffset(left), GetLimbOffset(right))
                : 0;
        }

        public static void RightShiftSelf(Span<nuint> bits, int shift, out nuint carry)
        {
            Debug.Assert((uint)shift < BitsPerLimb);

            carry = 0;
            if (shift == 0 || bits.IsEmpty)
            {
                return;
            }

            int back = BitsPerLimb - shift;

            carry = bits[0] << back;

            Span<nuint> remaining = bits;

            while (Vector512.IsHardwareAccelerated && remaining.Length >= Vector512<nuint>.Count + 1)
            {
                Vector512<nuint> current = Vector512.Create(remaining) >> shift;
                Vector512<nuint> carries = Vector512.Create(remaining.Slice(1)) << back;

                Vector512<nuint> newValue = current | carries;

                newValue.CopyTo(remaining);
                remaining = remaining.Slice(Vector512<nuint>.Count);
            }

            while (Vector256.IsHardwareAccelerated && remaining.Length >= Vector256<nuint>.Count + 1)
            {
                Vector256<nuint> current = Vector256.Create(remaining) >> shift;
                Vector256<nuint> carries = Vector256.Create(remaining.Slice(1)) << back;

                Vector256<nuint> newValue = current | carries;

                newValue.CopyTo(remaining);
                remaining = remaining.Slice(Vector256<nuint>.Count);
            }

            while (Vector128.IsHardwareAccelerated && remaining.Length >= Vector128<nuint>.Count + 1)
            {
                Vector128<nuint> current = Vector128.Create(remaining) >> shift;
                Vector128<nuint> carries = Vector128.Create(remaining.Slice(1)) << back;

                Vector128<nuint> newValue = current | carries;

                newValue.CopyTo(remaining);
                remaining = remaining.Slice(Vector128<nuint>.Count);
            }

            for (int i = 0; i < remaining.Length - 1; i++)
            {
                remaining[i] = (remaining[i] >> shift) | (remaining[i + 1] << back);
            }
            remaining[remaining.Length - 1] >>= shift;
        }

        internal static void DivideByPowerOfTwo(ReadOnlySpan<nuint> left, int exponent, Span<nuint> quotient)
        {
            int limbShift = Math.DivRem(exponent, BitsPerLimb, out int smallShift);
            ReadOnlySpan<nuint> source = left[limbShift..];

            if (smallShift == 0)
            {
                source[..quotient.Length].CopyTo(quotient);
                return;
            }

            int backShift = BitsPerLimb - smallShift;

            for (int i = 0; i < quotient.Length; i++)
            {
                nuint upper = (i + 1 < source.Length) ? source[i + 1] : 0;
                quotient[i] = (source[i] >> smallShift) | (upper << backShift);
            }
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

            if (left[0] == 0 || right[0] == 0)
            {
                int leftOffset = left.IndexOfAnyExcept((nuint)0);
                int rightOffset = right.IndexOfAnyExcept((nuint)0);

                if (leftOffset < 0 || rightOffset < 0)
                {
                    bits.Clear();
                    (leftOffset < 0 ? right : left).CopyTo(bits);
                    return;
                }

                if (Math.Max(leftOffset, rightOffset) >= 32)
                {
                    AddWithLimbOffsets(left, leftOffset, right, rightOffset, bits);
                    return;
                }
            }

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

        private static void AddWithLimbOffsets(
            ReadOnlySpan<nuint> left,
            int leftOffset,
            ReadOnlySpan<nuint> right,
            int rightOffset,
            Span<nuint> bits)
        {
            Debug.Assert(leftOffset > 0 || rightOffset > 0);
            Debug.Assert(leftOffset == GetLimbOffset(left));
            Debug.Assert(rightOffset == GetLimbOffset(right));
            Debug.Assert(bits.Length == Math.Max(left.Length, right.Length) + 1);

            ReadOnlySpan<nuint> low = left;
            int lowOffset = leftOffset;
            ReadOnlySpan<nuint> high = right;
            int highOffset = rightOffset;

            if (lowOffset > highOffset)
            {
                low = right;
                lowOffset = rightOffset;
                high = left;
                highOffset = leftOffset;
            }

            bits.Clear();
            low.Slice(lowOffset, Math.Min(highOffset, low.Length) - lowOffset).CopyTo(bits[lowOffset..]);

            if (low.Length <= highOffset)
            {
                high[highOffset..].CopyTo(bits[highOffset..]);
                return;
            }

            ReadOnlySpan<nuint> lowOverlap = low[highOffset..];
            ReadOnlySpan<nuint> highMagnitude = high[highOffset..];
            Span<nuint> resultMagnitude = bits.Slice(
                highOffset, Math.Max(lowOverlap.Length, highMagnitude.Length) + 1);

            if (lowOverlap.Length < highMagnitude.Length)
            {
                Add(highMagnitude, lowOverlap, resultMagnitude);
            }
            else
            {
                Add(lowOverlap, highMagnitude, resultMagnitude);
            }
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

            int commonOffset = GetCommonLimbOffset(left, right);
            if (commonOffset != 0)
            {
                bits[..commonOffset].Clear();
                Subtract(left[commonOffset..], right[commonOffset..], bits[commonOffset..]);
                return;
            }

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

        public static void MultiplyNaive(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits)
        {
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(right.IsEmpty || bits.Length >= left.Length + right.Length);

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

        internal static void MultiplyNaiveSparse(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits)
        {
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(right.IsEmpty || bits.Length >= left.Length + right.Length);

            for (int i = 0; i < right.Length; i++)
            {
                nuint multiplier = right[i];

                if (multiplier == 0)
                {
                    continue;
                }

                if (multiplier == 1)
                {
                    AddSelf(bits[i..], left);
                    continue;
                }

                nuint carry = MulAdd1(bits.Slice(i), left, multiplier);
                bits[i + left.Length] = carry;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsRepeatedLimbCandidate(ReadOnlySpan<nuint> value)
        {
            return value.Length >= 4
                && value[0] != 0
                && value[0] == value[1];
        }

        internal static bool TryMultiplyRepeatedLimb(
            ReadOnlySpan<nuint> left,
            ReadOnlySpan<nuint> right,
            Span<nuint> bits)
        {
            if (left.IsEmpty
                || right.Length < 4)
            {
                return false;
            }

            nuint repeatedLimb = right[0];

            if (right.ContainsAnyExcept(repeatedLimb))
            {
                return false;
            }

            int repeatedLength = right.Length;

            if (repeatedLimb == nuint.MaxValue)
            {
                left.CopyTo(bits[repeatedLength..]);
                SubtractSelf(bits, left);
                return true;
            }

            int productLength = left.Length + repeatedLength;
            uint convolutionLength = (uint)Math.Min(left.Length, repeatedLength);

            if (nint.Size == 8)
            {
                if (((UInt128)(ulong)repeatedLimb * convolutionLength) > ulong.MaxValue)
                {
                    return false;
                }

                UInt128 window = 0;
                UInt128 carry = 0;

                for (int i = 0; i < productLength - 1; i++)
                {
                    if (i < left.Length)
                    {
                        window += (ulong)left[i];
                    }

                    if (i >= repeatedLength)
                    {
                        window -= (ulong)left[i - repeatedLength];
                    }

                    UInt128 total = (window * (ulong)repeatedLimb) + carry;
                    bits[i] = (nuint)(ulong)total;
                    carry = total >> 64;
                }

                bits[productLength - 1] = (nuint)(ulong)carry;
            }
            else
            {
                if (((ulong)(uint)repeatedLimb * convolutionLength) > uint.MaxValue)
                {
                    return false;
                }

                ulong window = 0;
                ulong carry = 0;

                for (int i = 0; i < productLength - 1; i++)
                {
                    if (i < left.Length)
                    {
                        window += (uint)left[i];
                    }

                    if (i >= repeatedLength)
                    {
                        window -= (uint)left[i - repeatedLength];
                    }

                    ulong total = (window * (uint)repeatedLimb) + carry;
                    bits[i] = (nuint)(uint)total;
                    carry = total >> 32;
                }

                bits[productLength - 1] = (nuint)(uint)carry;
            }

            return true;
        }

        internal static bool TryMultiplyShiftedRepeatedLimbOperands(
            ReadOnlySpan<nuint> left,
            ReadOnlySpan<nuint> right,
            Span<nuint> bits)
        {
            return (IsShiftedRepeatedLimbCandidate(right)
                    && TryMultiplyShiftedRepeatedLimb(left, right, bits))
                || (IsShiftedRepeatedLimbCandidate(left)
                    && TryMultiplyShiftedRepeatedLimb(right, left, bits));

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsShiftedRepeatedLimbCandidate(ReadOnlySpan<nuint> value)
        {
            return value.Length >= 4
                && value[0] != 0
                && value[0] != value[1]
                && value[1] == value[2];
        }

        internal static bool TryMultiplyShiftedRepeatedLimb(
            ReadOnlySpan<nuint> left,
            ReadOnlySpan<nuint> right,
            Span<nuint> bits)
        {
            if (left.IsEmpty
                || right.Length < 4
                || !TryGetShiftedRepeatedLimb(right, out nuint repeatedLimb, out int repeatedLength, out int shift))
            {
                return false;
            }

            int productLength = left.Length + repeatedLength;

            if (repeatedLimb == nuint.MaxValue)
            {
                left.CopyTo(bits[repeatedLength..]);
                SubtractSelf(bits, left);
                ShiftProduct(bits, productLength, shift);
                return true;
            }

            uint convolutionLength = (uint)Math.Min(left.Length, repeatedLength);
            nuint shiftCarry = 0;
            int backShift = BitsPerLimb - shift;

            if (nint.Size == 8)
            {
                if (((UInt128)(ulong)repeatedLimb * convolutionLength) > ulong.MaxValue)
                {
                    return false;
                }

                UInt128 window = 0;
                UInt128 carry = 0;

                for (int i = 0; i < productLength - 1; i++)
                {
                    if (i < left.Length)
                    {
                        window += (ulong)left[i];
                    }

                    if (i >= repeatedLength)
                    {
                        window -= (ulong)left[i - repeatedLength];
                    }

                    UInt128 total = (window * (ulong)repeatedLimb) + carry;
                    nuint digit = (nuint)(ulong)total;
                    bits[i] = (digit << shift) | shiftCarry;
                    shiftCarry = digit >> backShift;
                    carry = total >> 64;
                }

                nuint finalDigit = (nuint)(ulong)carry;
                bits[productLength - 1] = (finalDigit << shift) | shiftCarry;
                shiftCarry = finalDigit >> backShift;
            }
            else
            {
                if (((ulong)(uint)repeatedLimb * convolutionLength) > uint.MaxValue)
                {
                    return false;
                }

                ulong window = 0;
                ulong carry = 0;

                for (int i = 0; i < productLength - 1; i++)
                {
                    if (i < left.Length)
                    {
                        window += (uint)left[i];
                    }

                    if (i >= repeatedLength)
                    {
                        window -= (uint)left[i - repeatedLength];
                    }

                    ulong total = (window * (uint)repeatedLimb) + carry;
                    nuint digit = (nuint)(uint)total;
                    bits[i] = (digit << shift) | shiftCarry;
                    shiftCarry = digit >> backShift;
                    carry = total >> 32;
                }

                nuint finalDigit = (nuint)(uint)carry;
                bits[productLength - 1] = (finalDigit << shift) | shiftCarry;
                shiftCarry = finalDigit >> backShift;
            }

            if (productLength < bits.Length)
            {
                bits[productLength] = shiftCarry;
            }
            else
            {
                Debug.Assert(shiftCarry == 0);
            }

            return true;

            static bool TryGetShiftedRepeatedLimb(
                ReadOnlySpan<nuint> value,
                out nuint repeatedLimb,
                out int repeatedLength,
                out int shift)
            {
                repeatedLength = value.Length;
                repeatedLimb = 0;
                shift = BitOperations.TrailingZeroCount(value[0]);

                if (shift == 0)
                {
                    return false;
                }

                int backShift = BitsPerLimb - shift;

                if ((value[^1] >> shift) == 0)
                {
                    repeatedLength--;
                }

                if (repeatedLength < 4)
                {
                    return false;
                }

                repeatedLimb = (value[0] >> shift) | (value[1] << backShift);

                for (int i = 1; i < repeatedLength; i++)
                {
                    nuint upper = (i + 1 < value.Length) ? value[i + 1] : 0;
                    nuint limb = (value[i] >> shift) | (upper << backShift);

                    if (limb != repeatedLimb)
                    {
                        return false;
                    }
                }

                return true;
            }

            static void ShiftProduct(Span<nuint> bits, int length, int shift)
            {
                int backShift = BitsPerLimb - shift;
                nuint carry = 0;

                for (int i = 0; i < length; i++)
                {
                    nuint digit = bits[i];
                    bits[i] = (digit << shift) | carry;
                    carry = digit >> backShift;
                }

                if (length < bits.Length)
                {
                    bits[length] = carry;
                }
                else
                {
                    Debug.Assert(carry == 0);
                }
            }
        }

        public static void Multiply(ReadOnlySpan<nuint> left, nuint right, Span<nuint> bits)
        {
            Debug.Assert(bits.Length == left.Length + 1);

            if (right == 0)
            {
                bits.Clear();
                return;
            }

            if (!left.IsEmpty && left[0] == 0)
            {
                int offset = left.IndexOfAnyExcept((nuint)0);
                if (offset < 0)
                {
                    bits.Clear();
                    return;
                }

                bits[..offset].Clear();
                Multiply(left[offset..], right, bits[offset..]);
                return;
            }

            if (BitOperations.IsPow2(right))
            {
                MultiplyByPowerOfTwo(left, BitOperations.TrailingZeroCount(right), bits);
                return;
            }

            nuint carry = Mul1(bits, left, right);
            bits[left.Length] = carry;
        }

        private static void MultiplyByPowerOfTwo(ReadOnlySpan<nuint> value, int exponent, Span<nuint> bits)
        {
            int limbShift = Math.DivRem(exponent, BitsPerLimb, out int smallShift);
            Span<nuint> shifted = bits.Slice(limbShift, value.Length);
            value.CopyTo(shifted);

            if (smallShift == 0)
            {
                bits[limbShift + value.Length] = 0;
                return;
            }

            int backShift = BitsPerLimb - smallShift;
            nuint carry = 0;

            for (int i = 0; i < shifted.Length; i++)
            {
                nuint current = shifted[i];
                shifted[i] = (current << smallShift) | carry;
                carry = current >> backShift;
            }

            bits[limbShift + value.Length] = carry;
        }

        public static void Divide(ReadOnlySpan<nuint> left, nuint right, Span<nuint> quotient, out nuint remainder)
        {
            InitializeForDebug(quotient);

            if (BitOperations.IsPow2(right))
            {
                DivideByPowerOfTwo(left, BitOperations.TrailingZeroCount(right), quotient);
                remainder = left[0] & (right - 1);
                return;
            }

            nuint carry = 0;

            if (!ShouldUseSpecializedScalarDivision(left.Length, right))
            {
                DivideDirect(left, right, quotient, ref carry);
                remainder = carry;
                return;
            }

            DivideCore(left, right, quotient, ref carry);
            remainder = carry;
        }

        public static void Divide(ReadOnlySpan<nuint> left, nuint right, Span<nuint> quotient)
        {
            InitializeForDebug(quotient);

            if (BitOperations.IsPow2(right))
            {
                DivideByPowerOfTwo(left, BitOperations.TrailingZeroCount(right), quotient);
                return;
            }

            nuint carry = 0;

            if (!ShouldUseSpecializedScalarDivision(left.Length, right))
            {
                DivideDirect(left, right, quotient, ref carry);
                return;
            }

            DivideCore(left, right, quotient, ref carry);
        }

        private static bool ShouldUseSpecializedScalarDivision(int length, nuint divisor)
        {
            if (length < 8)
            {
                return false;
            }

            int shift = BitOperations.TrailingZeroCount(divisor);
            nuint oddDivisor = divisor >> shift;
            return oddDivisor is 3 or 5 or 7
                || ShouldUseInvariantDivisor(length, oddDivisor, shift);
        }

        private static void DivideDirect(
            ReadOnlySpan<nuint> left,
            nuint right,
            Span<nuint> quotient,
            ref nuint carry)
        {
            for (int i = left.Length - 1; i >= 0; i--)
            {
                quotient[i] = DivRem(carry, left[i], right, out carry);
            }
        }

        private static void RemainderDirect(ReadOnlySpan<nuint> left, nuint right, ref nuint carry)
        {
            for (int i = left.Length - 1; i >= 0; i--)
            {
                DivRem(carry, left[i], right, out carry);
            }
        }

        private static void DivideCore(ReadOnlySpan<nuint> left, nuint right, Span<nuint> quotient, ref nuint carry)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(quotient.Length == left.Length);

            InitializeForDebug(quotient);

            int shift = BitOperations.TrailingZeroCount(right);
            nuint oddDivisor = right >> shift;

            if (left.Length >= 4)
            {
                switch (oddDivisor)
                {
                    case 3:
                        DivideSmallPrime<Divisor3>(left, shift, quotient, ref carry);
                        return;

                    case 5:
                        DivideSmallPrime<Divisor5>(left, shift, quotient, ref carry);
                        return;

                    case 7:
                        DivideSmallPrime<Divisor7>(left, shift, quotient, ref carry);
                        return;
                }
            }

            if (ShouldUseInvariantDivisor(left.Length, oddDivisor, shift))
            {
                var divisor = new InvariantDivisor(oddDivisor);
                DivideInvariant(left, divisor, shift, quotient, ref carry);
                return;
            }

            for (int i = left.Length - 1; i >= 0; i--)
            {
                quotient[i] = DivRem(carry, left[i], right, out nuint rem);
                carry = rem;
            }
        }

        private interface ISmallPrimeDivisor
        {
            static abstract uint Value { get; }

            static abstract nuint GetAdjustment(nuint value);
        }

        private readonly struct Divisor3 : ISmallPrimeDivisor
        {
            public static uint Value => 3;

            public static nuint GetAdjustment(nuint value) => value >= 3 ? (nuint)1 : 0;
        }

        private readonly struct Divisor5 : ISmallPrimeDivisor
        {
            public static uint Value => 5;

            public static nuint GetAdjustment(nuint value) => value >= 5 ? (nuint)1 : 0;
        }

        private readonly struct Divisor7 : ISmallPrimeDivisor
        {
            public static uint Value => 7;

            public static nuint GetAdjustment(nuint value)
            {
                if (nint.Size == 8)
                {
                    return value >= 14 ? (nuint)2 : value >= 7 ? (nuint)1 : 0;
                }

                return value >= 28 ? (nuint)4
                    : value >= 21 ? (nuint)3
                    : value >= 14 ? (nuint)2
                    : value >= 7 ? (nuint)1
                    : 0;
            }
        }

        private static void DivideSmallPrime<TDivisor>(
            ReadOnlySpan<nuint> left,
            int shift,
            Span<nuint> quotient,
            ref nuint carry)
            where TDivisor : struct, ISmallPrimeDivisor
        {
            if (shift == 0)
            {
                DivideSmallPrime<TDivisor>(left, quotient, ref carry);
                return;
            }

            Debug.Assert(quotient.IsEmpty || quotient.Length == left.Length);

            nuint quotientScale = nuint.MaxValue / TDivisor.Value;
            nuint remainderScale = (nuint.MaxValue % TDivisor.Value) + 1;
            bool writeQuotient = !quotient.IsEmpty;
            nuint leading = carry;
            carry >>= shift;

            for (int i = left.Length - 1; i >= 0; i--)
            {
                nuint digit = GetShiftedDigit(left, i, shift, leading);
                nuint currentCarry = carry;
                nuint result = digit / TDivisor.Value;
                nuint remainder = digit - (result * TDivisor.Value);
                nuint adjustedRemainder = remainder + (currentCarry * remainderScale);
                nuint adjustment = TDivisor.GetAdjustment(adjustedRemainder);

                if (writeQuotient)
                {
                    quotient[i] = (currentCarry * quotientScale) + result + adjustment;
                }

                carry = adjustedRemainder - (adjustment * TDivisor.Value);
            }

            carry = RestoreShiftedRemainder(left, carry, shift);
        }

        private static void DivideSmallPrime<TDivisor>(
            ReadOnlySpan<nuint> left,
            Span<nuint> quotient,
            ref nuint carry)
            where TDivisor : struct, ISmallPrimeDivisor
        {
            Debug.Assert(quotient.IsEmpty || quotient.Length == left.Length);

            nuint quotientScale = nuint.MaxValue / TDivisor.Value;
            nuint remainderScale = (nuint.MaxValue % TDivisor.Value) + 1;
            bool writeQuotient = !quotient.IsEmpty;

            for (int i = left.Length - 1; i >= 0; i--)
            {
                nuint digit = left[i];
                nuint currentCarry = carry;
                nuint result = digit / TDivisor.Value;
                nuint remainder = digit - (result * TDivisor.Value);
                nuint adjustedRemainder = remainder + (currentCarry * remainderScale);
                nuint adjustment = TDivisor.GetAdjustment(adjustedRemainder);

                if (writeQuotient)
                {
                    quotient[i] = (currentCarry * quotientScale) + result + adjustment;
                }

                carry = adjustedRemainder - (adjustment * TDivisor.Value);
            }
        }

        private readonly struct InvariantDivisor
        {
            private const byte AddMarker = 0x80;

            private readonly nuint _divisor;
            private readonly nuint _magic;
            private readonly byte _more;

            public InvariantDivisor(nuint divisor)
            {
                Debug.Assert(divisor >= 3);
                Debug.Assert(!BitOperations.IsPow2(divisor));

                _divisor = divisor;

                int floorLog2 = BitsPerLimb - 1 - (int)nuint.LeadingZeroCount(divisor);
                nuint proposedMagic;
                nuint remainder;

                if (nint.Size == 8)
                {
                    UInt128 numerator = UInt128.One << (64 + floorLog2);
                    proposedMagic = (nuint)(ulong)(numerator / divisor);
                    remainder = (nuint)(ulong)(numerator - ((UInt128)(ulong)proposedMagic * divisor));
                }
                else
                {
                    ulong numerator = 1UL << (32 + floorLog2);
                    proposedMagic = (nuint)(uint)(numerator / divisor);
                    remainder = (nuint)(uint)(numerator - ((ulong)proposedMagic * divisor));
                }

                nuint distance = divisor - remainder;

                if (distance < ((nuint)1 << floorLog2))
                {
                    _more = (byte)floorLog2;
                }
                else
                {
                    proposedMagic += proposedMagic;
                    nuint twiceRemainder = remainder + remainder;

                    if (twiceRemainder >= divisor || twiceRemainder < remainder)
                    {
                        proposedMagic++;
                    }

                    _more = (byte)(floorLog2 | AddMarker);
                }

                _magic = proposedMagic + 1;
            }

            public nuint DivRem(nuint value, out nuint remainder)
            {
                nuint quotient;

                if (nint.Size == 8)
                {
                    quotient = (nuint)Math.BigMul((ulong)_magic, (ulong)value, out _);
                }
                else
                {
                    quotient = (nuint)(uint)(((ulong)_magic * value) >> 32);
                }

                if ((_more & AddMarker) != 0)
                {
                    quotient = ((value - quotient) >> 1) + quotient;
                }

                quotient >>= _more & ~AddMarker;
                remainder = value - (quotient * _divisor);

                return quotient;
            }
        }

        private static bool CanUseInvariantDivisor(nuint divisor) =>
            divisor >= 3 && (nint.Size == 8 ? divisor <= uint.MaxValue : divisor <= ushort.MaxValue);

        private static bool ShouldUseInvariantDivisor(int length, nuint divisor, int shift)
        {
            if (!CanUseInvariantDivisor(divisor))
            {
                return false;
            }

            int threshold = shift != 0 ? 8
                : divisor >= 343 ? 16
                : divisor >= 27 ? 32
                : 128;
            return length >= threshold;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint GetShiftedDigit(ReadOnlySpan<nuint> value, int index, int shift, nuint leading)
        {
            nuint digit = value[index];

            if (shift != 0)
            {
                digit >>= shift;
                nuint upper = index + 1 < value.Length ? value[index + 1] : leading;
                digit |= upper << (BitsPerLimb - shift);
            }

            return digit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint RestoreShiftedRemainder(ReadOnlySpan<nuint> value, nuint remainder, int shift) =>
            shift == 0 ? remainder : (remainder << shift) | (value[0] & (((nuint)1 << shift) - 1));

        private static void DivideInvariant(
            ReadOnlySpan<nuint> left,
            InvariantDivisor divisor,
            int shift,
            Span<nuint> quotient,
            ref nuint carry)
        {
            Debug.Assert(quotient.IsEmpty || quotient.Length == left.Length);

            bool writeQuotient = !quotient.IsEmpty;
            nuint leading = carry;
            carry >>= shift;

            for (int i = left.Length - 1; i >= 0; i--)
            {
                nuint digit = GetShiftedDigit(left, i, shift, leading);

                if (nint.Size == 8)
                {
                    nuint value = (carry << 32) | (digit >> 32);
                    nuint upper = divisor.DivRem(value, out carry);

                    value = (carry << 32) | (uint)digit;
                    nuint lower = divisor.DivRem(value, out carry);
                    if (writeQuotient)
                    {
                        quotient[i] = (upper << 32) | lower;
                    }
                }
                else
                {
                    nuint value = (carry << 16) | (digit >> 16);
                    nuint upper = divisor.DivRem(value, out carry);

                    value = (carry << 16) | (ushort)digit;
                    nuint lower = divisor.DivRem(value, out carry);
                    if (writeQuotient)
                    {
                        quotient[i] = (upper << 16) | lower;
                    }
                }
            }

            carry = RestoreShiftedRemainder(left, carry, shift);
        }

        public static nuint Remainder(ReadOnlySpan<nuint> left, nuint right)
        {
            Debug.Assert(left.Length >= 1);

            if (BitOperations.IsPow2(right))
            {
                return left[0] & (right - 1);
            }

            nuint remainder = 0;

            if (!ShouldUseSpecializedScalarDivision(left.Length, right))
            {
                RemainderDirect(left, right, ref remainder);
                return remainder;
            }

            int shift = BitOperations.TrailingZeroCount(right);
            nuint oddDivisor = right >> shift;

            nuint invariantRemainder = 0;

            if (left.Length >= 4)
            {
                switch (oddDivisor)
                {
                    case 3:
                        DivideSmallPrime<Divisor3>(left, shift, default, ref invariantRemainder);
                        return invariantRemainder;

                    case 5:
                        DivideSmallPrime<Divisor5>(left, shift, default, ref invariantRemainder);
                        return invariantRemainder;

                    case 7:
                        DivideSmallPrime<Divisor7>(left, shift, default, ref invariantRemainder);
                        return invariantRemainder;
                }
            }

            if (ShouldUseInvariantDivisor(left.Length, oddDivisor, shift))
            {
                var divisor = new InvariantDivisor(oddDivisor);
                DivideInvariant(left, divisor, shift, default, ref invariantRemainder);
                return invariantRemainder;
            }

            nuint carry = 0;
            for (int i = left.Length - 1; i >= 0; i--)
            {
                DivRem(carry, left[i], right, out carry);
            }

            return carry;
        }

        internal static void DivideGrammarSchoolSpecial(Span<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> quotient)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(right[0] == 0);
            Debug.Assert(
                quotient.Length == 0
                || quotient.Length == left.Length - right.Length + 1
                || (CompareActual(left.Slice(left.Length - right.Length), right) < 0 && quotient.Length == left.Length - right.Length));

            int commonOffset = left[0] == 0 ? GetCommonLimbOffset(left, right) : 0;
            if (commonOffset != 0)
            {
                Span<nuint> reducedLeft = left[commonOffset..];
                ReadOnlySpan<nuint> reducedRight = right[commonOffset..];

                if (reducedRight[0] == 0)
                {
                    DivideGrammarSchoolSpecial(reducedLeft, reducedRight, quotient);
                }
                else
                {
                    DivideGrammarSchool(reducedLeft, reducedRight, quotient);
                }
                return;
            }

            int rightOffset = GetLimbOffset(right);
            if (right.Length - rightOffset <= ShiftedDivisorMaxReducedLength)
            {
                DivideGrammarSchool(left[rightOffset..], right[rightOffset..], quotient);
                return;
            }

            DivideGrammarSchool(left, right, quotient);
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
