// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
#if DEBUG
        // Mutable for unit testing...
        internal static
#else
        internal const
#endif
        int StackAllocThreshold = 64;

        // Number of bits per native-width limb: 32 on 32-bit, 64 on 64-bit.
        internal static int kcbitNuint
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => nint.Size * 8;
        }

        public static int Compare(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right)
        {
            Debug.Assert(left.Length <= right.Length || left.Slice(right.Length).ContainsAnyExcept((nuint)0));
            Debug.Assert(left.Length >= right.Length || right.Slice(left.Length).ContainsAnyExcept((nuint)0));

            if (left.Length != right.Length)
                return left.Length < right.Length ? -1 : 1;

            int iv = left.Length;
            while (--iv >= 0 && left[iv] == right[iv]) ;

            if (iv < 0)
                return 0;
            return left[iv] < right[iv] ? -1 : 1;
        }

        private static int CompareActual(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right)
        {
            if (left.Length != right.Length)
            {
                if (left.Length < right.Length)
                {
                    if (ActualLength(right.Slice(left.Length)) > 0)
                        return -1;
                    right = right.Slice(0, left.Length);
                }
                else
                {
                    if (ActualLength(left.Slice(right.Length)) > 0)
                        return +1;
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
                --length;
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
                UInt128 sum = (UInt128)(ulong)a + (ulong)b + (ulong)carryIn;
                carryOut = (nuint)(ulong)(sum >> 64);
                return (nuint)(ulong)sum;
            }
            else
            {
                ulong sum = (ulong)a + b + carryIn;
                carryOut = (nuint)(uint)(sum >> 32);
                return (nuint)(uint)sum;
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
                nuint b1 = (diff1 > a) ? (nuint)1 : (nuint)0;
                nuint diff2 = diff1 - borrowIn;
                nuint b2 = (diff2 > diff1) ? (nuint)1 : (nuint)0;
                borrowOut = b1 + b2;
                return diff2;
            }
            else
            {
                long diff = (long)a - (long)b - (long)borrowIn;
                borrowOut = (nuint)(uint)(-(int)(diff >> 32)); // 0 or 1
                return (nuint)(uint)diff;
            }
        }

        /// <summary>
        /// Performs widening multiply: a * b → (hi, lo). Used for schoolbook multiply inner loops.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nuint MulAdd(nuint a, nuint b, nuint addend, ref nuint carry)
        {
            if (nint.Size == 8)
            {
                UInt128 product = (UInt128)(ulong)a * (ulong)b + (ulong)addend + (ulong)carry;
                carry = (nuint)(ulong)(product >> 64);
                return (nuint)(ulong)product;
            }
            else
            {
                ulong product = (ulong)a * b + addend + carry;
                carry = (nuint)(uint)(product >> 32);
                return (nuint)(uint)product;
            }
        }

        /// <summary>
        /// Widening divide: (hi:lo) / divisor → (quotient, remainder).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nuint DivRem(nuint hi, nuint lo, nuint divisor, out nuint remainder)
        {
            if (nint.Size == 8)
            {
                UInt128 value = ((UInt128)(ulong)hi << 64) | (ulong)lo;
                UInt128 digit = value / (ulong)divisor;
                remainder = (nuint)(ulong)(value - digit * (ulong)divisor);
                return (nuint)(ulong)digit;
            }
            else
            {
                ulong value = ((ulong)hi << 32) | lo;
                ulong digit = value / divisor;
                remainder = (nuint)(uint)(value - digit * divisor);
                return (nuint)(uint)digit;
            }
        }

        /// <summary>
        /// Widening multiply of two limbs, returning just the product as (hi, lo).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nuint BigMul(nuint a, nuint b, out nuint low)
        {
            if (nint.Size == 8)
            {
                UInt128 product = (UInt128)(ulong)a * (ulong)b;
                low = (nuint)(ulong)product;
                return (nuint)(ulong)(product >> 64);
            }
            else
            {
                ulong product = (ulong)a * b;
                low = (nuint)(uint)product;
                return (nuint)(uint)(product >> 32);
            }
        }
    }
}
