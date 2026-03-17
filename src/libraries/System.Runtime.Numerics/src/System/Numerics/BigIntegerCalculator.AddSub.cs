// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        private const int CopyToThreshold = 8;

        private static void CopyTail(ReadOnlySpan<nuint> source, Span<nuint> dest, int start)
        {
            source.Slice(start).CopyTo(dest.Slice(start));
        }

        public static void Add(ReadOnlySpan<nuint> left, nuint right, Span<nuint> bits)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(bits.Length == left.Length + 1);

            Add(left, bits, ref MemoryMarshal.GetReference(bits), startIndex: 0, initialCarry: right);
        }

        public static void Add(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits)
        {
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(bits.Length == left.Length + 1);

            // Switching to managed references helps eliminating
            // index bounds check for all buffers.
            ref nuint resultPtr = ref MemoryMarshal.GetReference(bits);
            ref nuint rightPtr = ref MemoryMarshal.GetReference(right);
            ref nuint leftPtr = ref MemoryMarshal.GetReference(left);

            int i = 0;
            nuint carry = 0;

            // Executes the "grammar-school" algorithm for computing z = a + b.
            // While calculating z_i = a_i + b_i we take care of overflow:
            // Since a_i + b_i + c <= 2(base - 1) + 1 = 2*base - 1, our carry c
            // has always the value 1 or 0; hence, we're safe here.

            do
            {
                Unsafe.Add(ref resultPtr, i) = AddWithCarry(
                    Unsafe.Add(ref leftPtr, i),
                    Unsafe.Add(ref rightPtr, i),
                    carry, out carry);
                i++;
            } while (i < right.Length);

            Add(left, bits, ref resultPtr, startIndex: i, initialCarry: carry);
        }

        public static void AddSelf(Span<nuint> left, ReadOnlySpan<nuint> right)
        {
            Debug.Assert(left.Length >= right.Length);

            int i = 0;
            nuint carry = 0;

            // Switching to managed references helps eliminating
            // index bounds check...
            ref nuint leftPtr = ref MemoryMarshal.GetReference(left);

            // Executes the "grammar-school" algorithm for computing z = a + b.
            // Same as above, but we're writing the result directly to a and
            // stop execution, if we're out of b and c is already 0.

            for (; i < right.Length; i++)
            {
                Unsafe.Add(ref leftPtr, i) = AddWithCarry(
                    Unsafe.Add(ref leftPtr, i), right[i], carry, out carry);
            }
            for (; carry != 0 && i < left.Length; i++)
            {
                nuint sum = left[i] + carry;
                carry = (sum < carry) ? (nuint)1 : (nuint)0;
                left[i] = sum;
            }

            Debug.Assert(carry == 0);
        }

        public static void Subtract(ReadOnlySpan<nuint> left, nuint right, Span<nuint> bits)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(left[0] >= right || left.Length >= 2);
            Debug.Assert(bits.Length == left.Length);

            Subtract(left, bits, ref MemoryMarshal.GetReference(bits), startIndex: 0, initialBorrow: right);
        }

        public static void Subtract(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits)
        {
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(CompareActual(left, right) >= 0);
            Debug.Assert(bits.Length == left.Length);

            // Switching to managed references helps eliminating
            // index bounds check for all buffers.
            ref nuint resultPtr = ref MemoryMarshal.GetReference(bits);
            ref nuint rightPtr = ref MemoryMarshal.GetReference(right);
            ref nuint leftPtr = ref MemoryMarshal.GetReference(left);

            int i = 0;
            nuint borrow = 0;

            // Executes the "grammar-school" algorithm for computing z = a - b.

            do
            {
                Unsafe.Add(ref resultPtr, i) = SubWithBorrow(
                    Unsafe.Add(ref leftPtr, i),
                    Unsafe.Add(ref rightPtr, i),
                    borrow, out borrow);
                i++;
            } while (i < right.Length);

            Subtract(left, bits, ref resultPtr, startIndex: i, initialBorrow: borrow);
        }

        public static void SubtractSelf(Span<nuint> left, ReadOnlySpan<nuint> right)
        {
            Debug.Assert(left.Length >= right.Length);

            // Assertion failing per https://github.com/dotnet/runtime/issues/97780
            // Debug.Assert(CompareActual(left, right) >= 0);

            int i = 0;
            nuint borrow = 0;

            // Switching to managed references helps eliminating
            // index bounds check...
            ref nuint leftPtr = ref MemoryMarshal.GetReference(left);

            // Executes the "grammar-school" algorithm for computing z = a - b.
            // Same as above, but we're writing the result directly to a and
            // stop execution, if we're out of b and c is already 0.

            for (; i < right.Length; i++)
            {
                Unsafe.Add(ref leftPtr, i) = SubWithBorrow(
                    Unsafe.Add(ref leftPtr, i), right[i], borrow, out borrow);
            }
            for (; borrow != 0 && i < left.Length; i++)
            {
                nuint val = left[i];
                left[i] = val - borrow;
                borrow = (val < borrow) ? (nuint)1 : (nuint)0;
            }

            // Assertion failing per https://github.com/dotnet/runtime/issues/97780
            //Debug.Assert(borrow == 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Add(ReadOnlySpan<nuint> left, Span<nuint> bits, ref nuint resultPtr, int startIndex, nuint initialCarry)
        {
            // Executes the addition for one big and one single-limb integer.

            int i = startIndex;
            nuint carry = initialCarry;

            if (left.Length <= CopyToThreshold)
            {
                for (; i < left.Length; i++)
                {
                    nuint sum = left[i] + carry;
                    carry = (sum < carry) ? (nuint)1 : (nuint)0;
                    Unsafe.Add(ref resultPtr, i) = sum;
                }

                Unsafe.Add(ref resultPtr, left.Length) = carry;
            }
            else
            {
                for (; i < left.Length;)
                {
                    nuint sum = left[i] + carry;
                    carry = (sum < carry) ? (nuint)1 : (nuint)0;
                    Unsafe.Add(ref resultPtr, i) = sum;
                    i++;

                    // Once carry is set to 0 it can not be 1 anymore.
                    // So the tail of the loop is just the movement of argument values to result span.
                    if (carry == 0)
                    {
                        break;
                    }
                }

                Unsafe.Add(ref resultPtr, left.Length) = carry;

                if (i < left.Length)
                {
                    CopyTail(left, bits, i);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Subtract(ReadOnlySpan<nuint> left, Span<nuint> bits, ref nuint resultPtr, int startIndex, nuint initialBorrow)
        {
            // Executes the subtraction for one big and one single-limb integer.

            int i = startIndex;
            nuint borrow = initialBorrow;

            if (left.Length <= CopyToThreshold)
            {
                for (; i < left.Length; i++)
                {
                    nuint val = left[i];
                    nuint diff = val - borrow;
                    borrow = (diff > val) ? (nuint)1 : (nuint)0;
                    Unsafe.Add(ref resultPtr, i) = diff;
                }
            }
            else
            {
                for (; i < left.Length;)
                {
                    nuint val = left[i];
                    nuint diff = val - borrow;
                    borrow = (diff > val) ? (nuint)1 : (nuint)0;
                    Unsafe.Add(ref resultPtr, i) = diff;
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
