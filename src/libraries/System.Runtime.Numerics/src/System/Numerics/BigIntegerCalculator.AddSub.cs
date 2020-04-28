// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using static System.Runtime.InteropServices.MemoryMarshal;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        public static uint[] Add(uint[] left, uint right)
        {
            Debug.Assert(left != null);
            Debug.Assert(left.Length >= 1);

            // Executes the addition for one big and one 32-bit integer.
            // Thus, we've similar code than below, but there is no loop for
            // processing the 32-bit integer, since it's a single element.

            uint[] bits = new uint[left.Length + 1];

            Add(left, right, ref GetArrayDataReference(bits));

            return bits;
        }

        private static void Add(ReadOnlySpan<uint> left,
                                uint right,
                                ref uint bits)
        {
            long digit = (long)GetReference(left) + right;
            bits = unchecked((uint)digit);
            long carry = digit >> 32;

            for (int i = 1; i < left.Length; i++)
            {
                digit = Unsafe.Add(ref GetReference(left), i) + carry;
                Unsafe.Add(ref bits, i) = unchecked((uint)digit);
                carry = digit >> 32;
            }
            Unsafe.Add(ref bits, left.Length) = (uint)carry;
        }

        public static uint[] Add(uint[] left, uint[] right)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            Debug.Assert(left.Length >= right.Length);

            // Switching to managed pointers helps sparing
            // some nasty index calculations...

            uint[] bits = new uint[left.Length + 1];

            Add(left, right, bits);

            return bits;
        }

        private static void Add(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits)
        {
            Debug.Assert(left.Length >= 0);
            Debug.Assert(right.Length >= 0);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(bits.Length == left.Length + 1);

            // Executes the "grammar-school" algorithm for computing z = a + b.
            // While calculating z_i = a_i + b_i we take care of overflow:
            // Since a_i + b_i + c <= 2(2^32 - 1) + 1 = 2^33 - 1, our carry c
            // has always the value 1 or 0; hence, we're safe here.

            int i = 0;
            long carry = 0L;

            for (; i < right.Length; i++)
            {
                long digit = (Unsafe.Add(ref GetReference(left), i) + carry) + Unsafe.Add(ref GetReference(right), i);
                Unsafe.Add(ref GetReference(bits), i) = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for (; i < left.Length; i++)
            {
                long digit = Unsafe.Add(ref GetReference(left), i) + carry;
                Unsafe.Add(ref GetReference(bits), i) = unchecked((uint)digit);
                carry = digit >> 32;
            }
            Unsafe.Add(ref GetReference(bits), i) = (uint)carry;
        }

        private static void AddSelf(Span<uint> left, ReadOnlySpan<uint> right)
        {
            Debug.Assert(left.Length >= 0);
            Debug.Assert(right.Length >= 0);
            Debug.Assert(left.Length >= right.Length);

            // Executes the "grammar-school" algorithm for computing z = a + b.
            // Same as above, but we're writing the result directly to a and
            // stop execution, if we're out of b and c is already 0.

            int i = 0;
            long carry = 0L;
            ref uint leftElement = ref NullRef;
            for (; i < right.Length; i++)
            {
                leftElement = ref Unsafe.Add(ref GetReference(left), i);
                long digit = (leftElement + carry) + Unsafe.Add(ref GetReference(right), i);
                leftElement = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for (; carry != 0 && i < left.Length; i++)
            {
                leftElement = ref Unsafe.Add(ref GetReference(left), i);
                long digit = leftElement + carry;
                leftElement = (uint)digit;
                carry = digit >> 32;
            }

            Debug.Assert(carry == 0);
        }

        public static uint[] Subtract(uint[] left, uint right)
        {
            Debug.Assert(left != null);
            Debug.Assert(left.Length >= 1);
            Debug.Assert(left[0] >= right || left.Length >= 2);

            // Executes the subtraction for one big and one 32-bit integer.
            // Thus, we've similar code than below, but there is no loop for
            // processing the 32-bit integer, since it's a single element.

            uint[] bits = new uint[left.Length];

            Subtract(left, right, ref GetArrayDataReference(bits));

            return bits;
        }

        private static void Subtract(ReadOnlySpan<uint> left, uint right, ref uint bits)
        {
            long digit = (long)GetReference(left) - right;
            bits = unchecked((uint)digit);
            long carry = digit >> 32;

            for (int i = 1; i < left.Length; i++)
            {
                digit = Unsafe.Add(ref GetReference(left), i) + carry;
                Unsafe.Add(ref bits, i) = unchecked((uint)digit);
                carry = digit >> 32;
            }
        }

        public static uint[] Subtract(uint[] left, uint[] right)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(Compare(left, right) >= 0);

            // Switching to managed pointers helps sparing
            // some nasty index calculations...

            uint[] bits = new uint[left.Length];

            Subtract(left, right, bits);

            return bits;
        }

        private static void Subtract(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits)
        {
            Debug.Assert(left.Length >= 0);
            Debug.Assert(right.Length >= 0);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(Compare(left, right) >= 0);
            Debug.Assert(bits.Length == left.Length);

            // Executes the "grammar-school" algorithm for computing z = a - b.
            // While calculating z_i = a_i - b_i we take care of overflow:
            // Since a_i - b_i doesn't need any additional bit, our carry c
            // has always the value -1 or 0; hence, we're safe here.

            int i = 0;
            long carry = 0L;

            for (; i < right.Length; i++)
            {
                long digit = (Unsafe.Add(ref GetReference(left), i) + carry) - Unsafe.Add(ref GetReference(right), i);
                Unsafe.Add(ref GetReference(bits), i) = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for (; i < left.Length; i++)
            {
                long digit = Unsafe.Add(ref GetReference(left), i) + carry;
                Unsafe.Add(ref GetReference(bits), i) = (uint)digit;
                carry = digit >> 32;
            }

            Debug.Assert(carry == 0);
        }

        private static void SubtractSelf(Span<uint> left, ReadOnlySpan<uint> right)
        {
            Debug.Assert(left.Length >= 0);
            Debug.Assert(right.Length >= 0);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(Compare(left, right) >= 0);

            // Executes the "grammar-school" algorithm for computing z = a - b.
            // Same as above, but we're writing the result directly to a and
            // stop execution, if we're out of b and c is already 0.

            int i = 0;
            long carry = 0L;
            ref uint leftElement = ref NullRef;
            for (; i < right.Length; i++)
            {
                leftElement = ref Unsafe.Add(ref GetReference(left), i);
                long digit = (leftElement + carry) - Unsafe.Add(ref GetReference(right), i);
                leftElement = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for (; carry != 0 && i < left.Length; i++)
            {
                leftElement = ref Unsafe.Add(ref GetReference(left), i);
                long digit = leftElement + carry;
                leftElement = (uint)digit;
                carry = digit >> 32;
            }

            Debug.Assert(carry == 0);
        }
    }
}
