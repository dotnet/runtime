// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        /// <summary>
        /// Specifies the minimum number of elements required to trigger a copy operation using an optimized path.
        /// </summary>
        /// <remarks>
        /// This threshold is determined based on benchmarking and may be adjusted in the future to balance the overhead of copying versus the benefits of reduced loop iterations.
        /// </remarks>
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
    }
}
