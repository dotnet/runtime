// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        public static void Add(ReadOnlySpan<uint> left, uint right, Span<uint> bits)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(bits.Length == left.Length + 1);

            // Executes the addition for one big and one 32-bit integer.
            // Thus, we've similar code than below, but there is no loop for
            // processing the 32-bit integer, since it's a single element.

            long carry = right;

            for (int i = 0; i < left.Length; i++)
            {
                long digit = left[i] + carry;
                bits[i] = unchecked((uint)digit);
                carry = digit >> 32;
            }

            bits[left.Length] = (uint)carry;
        }

        public static void Add(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits)
        {
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(bits.Length == left.Length + 1);

            int i = 0;
            long carry = 0L;

            // Switching to managed references helps eliminating
            // index bounds check...
            ref uint leftPtr = ref MemoryMarshal.GetReference(left);
            ref uint resultPtr = ref MemoryMarshal.GetReference(bits);

            // Executes the "grammar-school" algorithm for computing z = a + b.
            // While calculating z_i = a_i + b_i we take care of overflow:
            // Since a_i + b_i + c <= 2(2^32 - 1) + 1 = 2^33 - 1, our carry c
            // has always the value 1 or 0; hence, we're safe here.

            for ( ; i < right.Length; i++)
            {
                long digit = (Unsafe.Add(ref leftPtr, i) + carry) + right[i];
                Unsafe.Add(ref resultPtr, i) = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for ( ; i < left.Length; i++)
            {
                long digit = left[i] + carry;
                Unsafe.Add(ref resultPtr, i) = unchecked((uint)digit);
                carry = digit >> 32;
            }
            Unsafe.Add(ref resultPtr, i) = (uint)carry;
        }

        private static void AddSelf(Span<uint> left, ReadOnlySpan<uint> right)
        {
            Debug.Assert(left.Length >= right.Length);

            int i = 0;
            long carry = 0L;

            // Switching to managed references helps eliminating
            // index bounds check...
            ref uint leftPtr = ref MemoryMarshal.GetReference(left);

            // Executes the "grammar-school" algorithm for computing z = a + b.
            // Same as above, but we're writing the result directly to a and
            // stop execution, if we're out of b and c is already 0.

            for ( ; i < right.Length; i++)
            {
                long digit = (Unsafe.Add(ref leftPtr, i) + carry) + right[i];
                Unsafe.Add(ref leftPtr, i) = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for ( ; carry != 0 && i < left.Length; i++)
            {
                long digit = left[i] + carry;
                left[i] = (uint)digit;
                carry = digit >> 32;
            }

            Debug.Assert(carry == 0);
        }

        public static void Subtract(ReadOnlySpan<uint> left, uint right, Span<uint> bits)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(left[0] >= right || left.Length >= 2);
            Debug.Assert(bits.Length == left.Length);

            // Executes the subtraction for one big and one 32-bit integer.
            // Thus, we've similar code than below, but there is no loop for
            // processing the 32-bit integer, since it's a single element.

            long carry = -right;

            for (int i = 0; i < left.Length; i++)
            {
                long digit = left[i] + carry;
                bits[i] = unchecked((uint)digit);
                carry = digit >> 32;
            }
        }

        public static void Subtract(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits)
        {
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(Compare(left, right) >= 0);
            Debug.Assert(bits.Length == left.Length);

            int i = 0;
            long carry = 0L;

            // Switching to managed references helps eliminating
            // index bounds check...
            ref uint leftPtr = ref MemoryMarshal.GetReference(left);
            ref uint resultPtr = ref MemoryMarshal.GetReference(bits);

            // Executes the "grammar-school" algorithm for computing z = a - b.
            // While calculating z_i = a_i - b_i we take care of overflow:
            // Since a_i - b_i doesn't need any additional bit, our carry c
            // has always the value -1 or 0; hence, we're safe here.

            for ( ; i < right.Length; i++)
            {
                long digit = (Unsafe.Add(ref leftPtr, i) + carry) - right[i];
                Unsafe.Add(ref resultPtr, i) = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for ( ; i < left.Length; i++)
            {
                long digit = left[i] + carry;
                Unsafe.Add(ref resultPtr, i) = (uint)digit;
                carry = digit >> 32;
            }

            Debug.Assert(carry == 0);
        }

        private static void SubtractSelf(Span<uint> left, ReadOnlySpan<uint> right)
        {
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(Compare(left, right) >= 0);

            int i = 0;
            long carry = 0L;

            // Switching to managed references helps eliminating
            // index bounds check...
            ref uint leftPtr = ref MemoryMarshal.GetReference(left);

            // Executes the "grammar-school" algorithm for computing z = a - b.
            // Same as above, but we're writing the result directly to a and
            // stop execution, if we're out of b and c is already 0.

            for (; i < right.Length; i++)
            {
                long digit = (Unsafe.Add(ref leftPtr, i) + carry) - right[i];
                Unsafe.Add(ref leftPtr, i) = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for (; carry != 0 && i < left.Length; i++)
            {
                long digit = left[i] + carry;
                left[i] = (uint)digit;
                carry = digit >> 32;
            }

            Debug.Assert(carry == 0);
        }
    }
}
