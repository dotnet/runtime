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
        public static uint[] Square(uint[] value)
        {
            Debug.Assert(value != null);

            // Switching to unsafe pointers helps sparing
            // some nasty index calculations...

            uint[] bits = new uint[value.Length + value.Length];

            Square(ref GetArrayDataReference(value), value.Length,
                   ref GetArrayDataReference(bits), bits.Length);

            return bits;
        }

        // Mutable for unit testing...
        private static int SquareThreshold = 32;
        private static int AllocationThreshold = 256;

        private static unsafe void Square(ref uint value, int valueLength,
                                          ref uint bits, int bitsLength)
        {
            Debug.Assert(valueLength >= 0);
            Debug.Assert(bitsLength == valueLength + valueLength);

            // Executes different algorithms for computing z = a * a
            // based on the actual length of a. If a is "small" enough
            // we stick to the classic "grammar-school" method; for the
            // rest we switch to implementations with less complexity
            // albeit more overhead (which needs to pay off!).

            // NOTE: useful thresholds needs some "empirical" testing,
            // which are smaller in DEBUG mode for testing purpose.

            if (valueLength < SquareThreshold)
            {
                // Squares the bits using the "grammar-school" method.
                // Envisioning the "rhombus" of a pen-and-paper calculation
                // we see that computing z_i+j += a_j * a_i can be optimized
                // since a_j * a_i = a_i * a_j (we're squaring after all!).
                // Thus, we directly get z_i+j += 2 * a_j * a_i + c.

                // ATTENTION: an ordinary multiplication is safe, because
                // z_i+j + a_j * a_i + c <= 2(2^32 - 1) + (2^32 - 1)^2 =
                // = 2^64 - 1 (which perfectly matches with ulong!). But
                // here we would need an UInt65... Hence, we split these
                // operation and do some extra shifts.
                ref uint elementPtr = ref value;
                for (int i = 0; i < valueLength; i++)
                {
                    ulong carry = 0UL;
                    for (int j = 0; j < i; j++)
                    {
                        elementPtr = ref Unsafe.Add(ref bits, i + j);
                        ulong digit1 = elementPtr + carry;
                        ulong digit2 = (ulong)Unsafe.Add(ref value, j) * Unsafe.Add(ref value, i);
                        elementPtr = unchecked((uint)(digit1 + (digit2 << 1)));
                        carry = (digit2 + (digit1 >> 1)) >> 31;
                    }
                    elementPtr = ref Unsafe.Add(ref value, i);
                    ulong digits = (ulong)elementPtr * elementPtr + carry;
                    elementPtr = ref Unsafe.Add(ref bits, i + i);
                    elementPtr = unchecked((uint)digits);
                    Unsafe.Add(ref elementPtr, 1) = (uint)(digits >> 32);
                }
            }
            else
            {
                // Based on the Toom-Cook multiplication we split value
                // into two smaller values, doing recursive squaring.
                // The special form of this multiplication, where we
                // split both operands into two operands, is also known
                // as the Karatsuba algorithm...

                // https://en.wikipedia.org/wiki/Toom-Cook_multiplication
                // https://en.wikipedia.org/wiki/Karatsuba_algorithm

                // Say we want to compute z = a * a ...

                // ... we need to determine our new length (just the half)
                int n = valueLength >> 1;
                int n2 = n << 1;

                // ... split value like a = (a_1 << n) + a_0
                ref uint valueLow = ref value;
                int valueLowLength = n;
                ref uint valueHigh = ref Unsafe.Add(ref value, n);
                int valueHighLength = valueLength - n;

                // ... prepare our result array (to reuse its memory)
                ref uint bitsLow = ref bits;
                int bitsLowLength = n2;
                ref uint bitsHigh = ref Unsafe.Add(ref bits, n2);
                int bitsHighLength = bitsLength - n2;

                // ... compute z_0 = a_0 * a_0 (squaring again!)
                Square(ref valueLow, valueLowLength,
                       ref bitsLow, bitsLowLength);

                // ... compute z_2 = a_1 * a_1 (squaring again!)
                Square(ref valueHigh, valueHighLength,
                       ref bitsHigh, bitsHighLength);

                int foldLength = valueHighLength + 1;
                int coreLength = foldLength + foldLength;

                bool stackAllocRequired = coreLength < AllocationThreshold;
                Span<uint> fold = stackAllocRequired ? stackalloc uint[foldLength] : new uint[foldLength];
                Span<uint> core = stackAllocRequired ? stackalloc uint[coreLength] : new uint[coreLength];

                if (stackAllocRequired)
                {
                    fold.Clear();
                    core.Clear();
                }

                // ... compute z_a = a_1 + a_0 (call it fold...)
                Add(ref valueHigh, valueHighLength,
                    ref valueLow, valueLowLength,
                    ref GetReference(fold), foldLength);

                // ... compute z_1 = z_a * z_a - z_0 - z_2
                Square(ref GetReference(fold), foldLength,
                        ref GetReference(core), coreLength);
                SubtractCore(ref bitsHigh, bitsHighLength,
                                ref bitsLow, bitsLowLength,
                                ref GetReference(core), coreLength);

                // ... and finally merge the result! :-)
                AddSelf(ref Unsafe.Add(ref bits, n), bitsLength - n, ref GetReference(core), coreLength);
            }
        }

        public static uint[] Multiply(uint[] left, uint right)
        {
            Debug.Assert(left != null);

            // Executes the multiplication for one big and one 32-bit integer.
            // Since every step holds the already slightly familiar equation
            // a_i * b + c <= 2^32 - 1 + (2^32 - 1)^2 < 2^64 - 1,
            // we are safe regarding to overflows.

            uint[] bits = new uint[left.Length + 1];

            Multiply(ref GetArrayDataReference(left), left.Length, right, ref GetArrayDataReference(bits));

            return bits;
        }

        private static void Multiply(ref uint left, int leftLength,
                                     uint right,
                                     ref uint bits)
        {
            int i = 0;
            ulong carry = 0UL;

            for ( ; i < leftLength; i++)
            {
                ulong digits = (ulong)Unsafe.Add(ref left, i) * right + carry;
                Unsafe.Add(ref bits, i) = unchecked((uint)digits);
                carry = digits >> 32;
            }
            Unsafe.Add(ref bits, i) = (uint)carry;
        }

        public static uint[] Multiply(uint[] left, uint[] right)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            Debug.Assert(left.Length >= right.Length);

            // Switching to unsafe pointers helps sparing
            // some nasty index calculations...

            uint[] bits = new uint[left.Length + right.Length];

            Multiply(ref GetArrayDataReference(left), left.Length,
                     ref GetArrayDataReference(right), right.Length,
                     ref GetArrayDataReference(bits), bits.Length);

            return bits;
        }

        // Mutable for unit testing...
        private static int MultiplyThreshold = 32;

        private static void Multiply(ref uint left, int leftLength,
                                            ref uint right, int rightLength,
                                            ref uint bits, int bitsLength)
        {
            Debug.Assert(leftLength >= 0);
            Debug.Assert(rightLength >= 0);
            Debug.Assert(leftLength >= rightLength);
            Debug.Assert(bitsLength == leftLength + rightLength);

            // Executes different algorithms for computing z = a * b
            // based on the actual length of b. If b is "small" enough
            // we stick to the classic "grammar-school" method; for the
            // rest we switch to implementations with less complexity
            // albeit more overhead (which needs to pay off!).

            // NOTE: useful thresholds needs some "empirical" testing,
            // which are smaller in DEBUG mode for testing purpose.

            if (rightLength < MultiplyThreshold)
            {
                // Multiplies the bits using the "grammar-school" method.
                // Envisioning the "rhombus" of a pen-and-paper calculation
                // should help getting the idea of these two loops...
                // The inner multiplication operations are safe, because
                // z_i+j + a_j * b_i + c <= 2(2^32 - 1) + (2^32 - 1)^2 =
                // = 2^64 - 1 (which perfectly matches with ulong!).

                ref uint elementPtr = ref left;
                for (int i = 0; i < rightLength; i++)
                {
                    ulong carry = 0UL;
                    for (int j = 0; j < leftLength; j++)
                    {
                        elementPtr = ref Unsafe.Add(ref bits, i + j);
                        ulong digits = elementPtr + carry
                            + (ulong)Unsafe.Add(ref left, j) * Unsafe.Add(ref right, i);
                        elementPtr = unchecked((uint)digits);
                        carry = digits >> 32;
                    }
                    Unsafe.Add(ref bits, i + leftLength) = (uint)carry;
                }
            }
            else
            {
                // Based on the Toom-Cook multiplication we split left/right
                // into two smaller values, doing recursive multiplication.
                // The special form of this multiplication, where we
                // split both operands into two operands, is also known
                // as the Karatsuba algorithm...

                // https://en.wikipedia.org/wiki/Toom-Cook_multiplication
                // https://en.wikipedia.org/wiki/Karatsuba_algorithm

                // Say we want to compute z = a * b ...

                // ... we need to determine our new length (just the half)
                int n = rightLength >> 1;
                int n2 = n << 1;

                // ... split left like a = (a_1 << n) + a_0
                ref uint leftLow = ref left;
                int leftLowLength = n;
                ref uint leftHigh = ref Unsafe.Add(ref left, n);
                int leftHighLength = leftLength - n;

                // ... split right like b = (b_1 << n) + b_0
                ref uint rightLow = ref right;
                int rightLowLength = n;
                ref uint rightHigh = ref Unsafe.Add(ref right, n);
                int rightHighLength = rightLength - n;

                // ... prepare our result array (to reuse its memory)
                ref uint bitsLow = ref bits;
                int bitsLowLength = n2;
                ref uint bitsHigh = ref Unsafe.Add(ref bits, n2);
                int bitsHighLength = bitsLength - n2;

                // ... compute z_0 = a_0 * b_0 (multiply again)
                Multiply(ref leftLow, leftLowLength,
                         ref rightLow, rightLowLength,
                         ref bitsLow, bitsLowLength);

                // ... compute z_2 = a_1 * b_1 (multiply again)
                Multiply(ref leftHigh, leftHighLength,
                         ref rightHigh, rightHighLength,
                         ref bitsHigh, bitsHighLength);

                int leftFoldLength = leftHighLength + 1;
                int rightFoldLength = rightHighLength + 1;
                int coreLength = leftFoldLength + rightFoldLength;

                bool stackAllocRequired = coreLength < AllocationThreshold;
                Span<uint> leftFold = stackAllocRequired ? stackalloc uint[leftFoldLength] : new uint[leftFoldLength];
                Span<uint> rightFold = stackAllocRequired ? stackalloc uint[rightFoldLength] : new uint[rightFoldLength];
                Span<uint> core = stackAllocRequired ? stackalloc uint[coreLength] : new uint[coreLength];

                if (stackAllocRequired)
                {
                    leftFold.Clear();
                    rightFold.Clear();
                    core.Clear();
                }

                // ... compute z_a = a_1 + a_0 (call it fold...)
                Add(ref leftHigh, leftHighLength,
                    ref leftLow, leftLowLength,
                    ref GetReference(leftFold), leftFoldLength);

                // ... compute z_b = b_1 + b_0 (call it fold...)
                Add(ref rightHigh, rightHighLength,
                    ref rightLow, rightLowLength,
                    ref GetReference(rightFold), rightFoldLength);

                // ... compute z_1 = z_a * z_b - z_0 - z_2
                Multiply(ref GetReference(leftFold), leftFoldLength,
                            ref GetReference(rightFold), rightFoldLength,
                            ref GetReference(core), coreLength);
                SubtractCore(ref bitsHigh, bitsHighLength,
                                ref bitsLow, bitsLowLength,
                                ref GetReference(core), coreLength);

                // ... and finally merge the result! :-)
                AddSelf(ref Unsafe.Add(ref bits, n), bitsLength - n, ref GetReference(core), coreLength);
            }
        }

        private static void SubtractCore(ref uint left, int leftLength,
                                                ref uint right, int rightLength,
                                                ref uint core, int coreLength)
        {
            Debug.Assert(leftLength >= 0);
            Debug.Assert(rightLength >= 0);
            Debug.Assert(coreLength >= 0);
            Debug.Assert(leftLength >= rightLength);
            Debug.Assert(coreLength >= leftLength);

            // Executes a special subtraction algorithm for the multiplication,
            // which needs to subtract two different values from a core value,
            // while core is always bigger than the sum of these values.

            // NOTE: we could do an ordinary subtraction of course, but we spare
            // one "run", if we do this computation within a single one...

            int i = 0;
            long carry = 0L;

            ref uint elementPtr = ref left;
            for (; i < rightLength; i++)
            {
                elementPtr = ref Unsafe.Add(ref core, i);
                long digit = (elementPtr + carry) - Unsafe.Add(ref left, i) - Unsafe.Add(ref right, i);
                elementPtr = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for (; i < leftLength; i++)
            {
                elementPtr = ref Unsafe.Add(ref core, i);
                long digit = (elementPtr + carry) - Unsafe.Add(ref left, i);
                elementPtr = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for (; carry != 0 && i < coreLength; i++)
            {
                elementPtr = ref Unsafe.Add(ref core, i);
                long digit = elementPtr + carry;
                elementPtr = (uint)digit;
                carry = digit >> 32;
            }
        }
    }
}
