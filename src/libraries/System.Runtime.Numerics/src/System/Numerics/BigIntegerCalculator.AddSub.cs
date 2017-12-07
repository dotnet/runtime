// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Security;

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

            long digit = (long)left[0] + right;
            bits[0] = unchecked((uint)digit);
            long carry = digit >> 32;

            for (int i = 1; i < left.Length; i++)
            {
                digit = left[i] + carry;
                bits[i] = unchecked((uint)digit);
                carry = digit >> 32;
            }
            bits[left.Length] = (uint)carry;

            return bits;
        }

        public static unsafe uint[] Add(uint[] left, uint[] right)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            Debug.Assert(left.Length >= right.Length);

            // Switching to unsafe pointers helps sparing
            // some nasty index calculations...

            uint[] bits = new uint[left.Length + 1];

            fixed (uint* l = left, r = right, b = &bits[0])
            {
                Add(l, left.Length,
                    r, right.Length,
                    b, bits.Length);
            }

            return bits;
        }

        private static unsafe void Add(uint* left, int leftLength,
                                       uint* right, int rightLength,
                                       uint* bits, int bitsLength)
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
                long digit = (left[i] + carry) + right[i];
                bits[i] = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for (; i < leftLength; i++)
            {
                long digit = left[i] + carry;
                bits[i] = unchecked((uint)digit);
                carry = digit >> 32;
            }
            bits[i] = (uint)carry;
        }

        private static unsafe void AddSelf(uint* left, int leftLength,
                                           uint* right, int rightLength)
        {
            Debug.Assert(leftLength >= 0);
            Debug.Assert(rightLength >= 0);
            Debug.Assert(leftLength >= rightLength);

            // Executes the "grammar-school" algorithm for computing z = a + b.
            // Same as above, but we're writing the result directly to a and
            // stop execution, if we're out of b and c is already 0.

            int i = 0;
            long carry = 0L;

            for (; i < rightLength; i++)
            {
                long digit = (left[i] + carry) + right[i];
                left[i] = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for (; carry != 0 && i < leftLength; i++)
            {
                long digit = left[i] + carry;
                left[i] = (uint)digit;
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

            long digit = (long)left[0] - right;
            bits[0] = unchecked((uint)digit);
            long carry = digit >> 32;

            for (int i = 1; i < left.Length; i++)
            {
                digit = left[i] + carry;
                bits[i] = unchecked((uint)digit);
                carry = digit >> 32;
            }

            return bits;
        }

        public static unsafe uint[] Subtract(uint[] left, uint[] right)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(Compare(left, right) >= 0);

            // Switching to unsafe pointers helps sparing
            // some nasty index calculations...

            uint[] bits = new uint[left.Length];

            fixed (uint* l = left, r = right, b = bits)
            {
                Subtract(l, left.Length,
                         r, right.Length,
                         b, bits.Length);
            }

            return bits;
        }

        private static unsafe void Subtract(uint* left, int leftLength, 
                                            uint* right, int rightLength,
                                            uint* bits, int bitsLength)
        {
            Debug.Assert(leftLength >= 0);
            Debug.Assert(rightLength >= 0);
            Debug.Assert(leftLength >= rightLength);
            Debug.Assert(Compare(left, leftLength, right, rightLength) >= 0);
            Debug.Assert(bitsLength == leftLength);

            // Executes the "grammar-school" algorithm for computing z = a - b.
            // While calculating z_i = a_i - b_i we take care of overflow:
            // Since a_i - b_i doesn't need any additional bit, our carry c
            // has always the value -1 or 0; hence, we're safe here.

            int i = 0;
            long carry = 0L;

            for (; i < rightLength; i++)
            {
                long digit = (left[i] + carry) - right[i];
                bits[i] = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for (; i < leftLength; i++)
            {
                long digit = left[i] + carry;
                bits[i] = (uint)digit;
                carry = digit >> 32;
            }

            Debug.Assert(carry == 0);
        }

        private static unsafe void SubtractSelf(uint* left, int leftLength,
                                                uint* right, int rightLength)
        {
            Debug.Assert(leftLength >= 0);
            Debug.Assert(rightLength >= 0);
            Debug.Assert(leftLength >= rightLength);
            Debug.Assert(Compare(left, leftLength, right, rightLength) >= 0);

            // Executes the "grammar-school" algorithm for computing z = a - b.
            // Same as above, but we're writing the result directly to a and
            // stop execution, if we're out of b and c is already 0.

            int i = 0;
            long carry = 0L;

            for (; i < rightLength; i++)
            {
                long digit = (left[i] + carry) - right[i];
                left[i] = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for (; carry != 0 && i < leftLength; i++)
            {
                long digit = left[i] + carry;
                left[i] = (uint)digit;
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

        private static unsafe int Compare(uint* left, int leftLength,
                                          uint* right, int rightLength)
        {
            Debug.Assert(leftLength >= 0);
            Debug.Assert(rightLength >= 0);

            if (leftLength < rightLength)
                return -1;
            if (leftLength > rightLength)
                return 1;

            for (int i = leftLength - 1; i >= 0; i--)
            {
                if (left[i] < right[i])
                    return -1;
                if (left[i] > right[i])
                    return 1;
            }

            return 0;
        }
    }
}
