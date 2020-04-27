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

            Add(ref GetArrayDataReference(left), left.Length,
                right,
                ref GetArrayDataReference(bits));

            return bits;
        }

        private static void Add(ref uint left, int leftLength,
                                uint right,
                                ref uint bits)
        {
            long digit = (long)left + right;
            bits = unchecked((uint)digit);
            long carry = digit >> 32;

            for (int i = 1; i < leftLength; i++)
            {
                digit = Unsafe.Add(ref left, i) + carry;
                Unsafe.Add(ref bits, i) = unchecked((uint)digit);
                carry = digit >> 32;
            }
            Unsafe.Add(ref bits, leftLength) = (uint)carry;
        }

        public static uint[] Add(uint[] left, uint[] right)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            Debug.Assert(left.Length >= right.Length);

            // Switching to managed pointers helps sparing
            // some nasty index calculations...

            uint[] bits = new uint[left.Length + 1];

            Add(ref GetArrayDataReference(left), left.Length,
                ref GetArrayDataReference(right), right.Length,
                ref GetArrayDataReference(bits), bits.Length);

            return bits;
        }

        private static void Add(ref uint left, int leftLength,
                                       ref uint right, int rightLength,
                                       ref uint bits, int bitsLength)
        {
            Debug.Assert(leftLength >= 0);
            Debug.Assert(rightLength >= 0);
            Debug.Assert(leftLength >= rightLength);
            Debug.Assert(bitsLength == leftLength + 1);

            // Executes the "grammar-school" algorithm for computing z = a + b.
            // While calculating z_i = a_i + b_i we take care of overflow:
            // Since a_i + b_i + c <= 2(2^32 - 1) + 1 = 2^33 - 1, our carry c
            // has always the value 1 or 0; hence, we're safe here.

            int i = 0;
            long carry = 0L;

            for (; i < rightLength; i++)
            {
                long digit = (Unsafe.Add(ref left, i) + carry) + Unsafe.Add(ref right, i);
                Unsafe.Add(ref bits, i) = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for (; i < leftLength; i++)
            {
                long digit = Unsafe.Add(ref left, i) + carry;
                Unsafe.Add(ref bits, i) = unchecked((uint)digit);
                carry = digit >> 32;
            }
            Unsafe.Add(ref bits, i) = (uint)carry;
        }

        private static void AddSelf(ref uint left, int leftLength,
                                           ref uint right, int rightLength)
        {
            Debug.Assert(leftLength >= 0);
            Debug.Assert(rightLength >= 0);
            Debug.Assert(leftLength >= rightLength);

            // Executes the "grammar-school" algorithm for computing z = a + b.
            // Same as above, but we're writing the result directly to a and
            // stop execution, if we're out of b and c is already 0.

            int i = 0;
            long carry = 0L;
            ref uint leftElement = ref left;
            for (; i < rightLength; i++)
            {
                leftElement = ref Unsafe.Add(ref left, i);
                long digit = (leftElement + carry) + Unsafe.Add(ref right, i);
                leftElement = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for (; carry != 0 && i < leftLength; i++)
            {
                leftElement = ref Unsafe.Add(ref left, i);
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

            Subtract(ref GetArrayDataReference(left), left.Length,
                     right,
                     ref GetArrayDataReference(bits));

            return bits;
        }

        private static void Subtract(ref uint left, int leftLength,
                                     uint right,
                                     ref uint bits)
        {
            long digit = (long)left - right;
            bits = unchecked((uint)digit);
            long carry = digit >> 32;

            for (int i = 1; i < leftLength; i++)
            {
                digit = Unsafe.Add(ref left, i) + carry;
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

            Subtract(ref GetArrayDataReference(left), left.Length,
                     ref GetArrayDataReference(right), right.Length,
                     ref GetArrayDataReference(bits), bits.Length);

            return bits;
        }

        private static void Subtract(ref uint left, int leftLength,
                                            ref uint right, int rightLength,
                                            ref uint bits, int bitsLength)
        {
            Debug.Assert(leftLength >= 0);
            Debug.Assert(rightLength >= 0);
            Debug.Assert(leftLength >= rightLength);
            Debug.Assert(Compare(ref left, leftLength, ref right, rightLength) >= 0);
            Debug.Assert(bitsLength == leftLength);

            // Executes the "grammar-school" algorithm for computing z = a - b.
            // While calculating z_i = a_i - b_i we take care of overflow:
            // Since a_i - b_i doesn't need any additional bit, our carry c
            // has always the value -1 or 0; hence, we're safe here.

            int i = 0;
            long carry = 0L;

            for (; i < rightLength; i++)
            {
                long digit = (Unsafe.Add(ref left, i) + carry) - Unsafe.Add(ref right, i);
                Unsafe.Add(ref bits, i) = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for (; i < leftLength; i++)
            {
                long digit = Unsafe.Add(ref left, i) + carry;
                Unsafe.Add(ref bits, i) = (uint)digit;
                carry = digit >> 32;
            }

            Debug.Assert(carry == 0);
        }

        private static void SubtractSelf(ref uint left, int leftLength,
                                                ref uint right, int rightLength)
        {
            Debug.Assert(leftLength >= 0);
            Debug.Assert(rightLength >= 0);
            Debug.Assert(leftLength >= rightLength);
            Debug.Assert(Compare(ref left, leftLength, ref right, rightLength) >= 0);

            // Executes the "grammar-school" algorithm for computing z = a - b.
            // Same as above, but we're writing the result directly to a and
            // stop execution, if we're out of b and c is already 0.

            int i = 0;
            long carry = 0L;
            ref uint leftElement = ref left;
            for (; i < rightLength; i++)
            {
                leftElement = ref Unsafe.Add(ref left, i);
                long digit = (leftElement + carry) - Unsafe.Add(ref right, i);
                leftElement = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for (; carry != 0 && i < leftLength; i++)
            {
                leftElement = ref Unsafe.Add(ref left, i);
                long digit = leftElement + carry;
                leftElement = (uint)digit;
                carry = digit >> 32;
            }

            Debug.Assert(carry == 0);
        }

        public static int Compare(uint[] left, uint[] right)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);

            if (left.Length < right.Length)
                return -1;
            if (left.Length > right.Length)
                return 1;

            for (int i = left.Length - 1; i >= 0; i--)
            {
                if (left[i] < right[i])
                    return -1;
                if (left[i] > right[i])
                    return 1;
            }

            return 0;
        }

        private static int Compare(ref uint left, int leftLength,
                                          ref uint right, int rightLength)
        {
            Debug.Assert(leftLength >= 0);
            Debug.Assert(rightLength >= 0);

            if (leftLength < rightLength)
                return -1;
            if (leftLength > rightLength)
                return 1;

            for (int i = leftLength - 1; i >= 0; i--)
            {
                if (Unsafe.Add(ref left, i) < Unsafe.Add(ref right, i))
                    return -1;
                if (Unsafe.Add(ref left, i) > Unsafe.Add(ref right, i))
                    return 1;
            }

            return 0;
        }
    }
}
