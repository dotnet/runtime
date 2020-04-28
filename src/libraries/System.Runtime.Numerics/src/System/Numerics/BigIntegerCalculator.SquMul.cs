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
                Span<uint> valueLow = CreateSpan(ref value, n);
                Span<uint> valueHigh = CreateSpan(ref Unsafe.Add(ref value, n), valueLength - n);

                // ... prepare our result array (to reuse its memory)
                Span<uint> bitsLow = CreateSpan(ref bits, n2);
                Span<uint> bitsHigh = CreateSpan(ref Unsafe.Add(ref bits, n2), bitsLength - n2);

                // ... compute z_0 = a_0 * a_0 (squaring again!)
                Square(ref GetReference(valueLow), valueLow.Length,
                       ref GetReference(bitsLow), bitsLow.Length);

                // ... compute z_2 = a_1 * a_1 (squaring again!)
                Square(ref GetReference(valueHigh), valueHigh.Length,
                       ref GetReference(bitsHigh), bitsHigh.Length);

                int foldLength = valueHigh.Length + 1;
                int coreLength = foldLength + foldLength;

                Span<uint> result = CreateSpan(ref Unsafe.Add(ref bits, n), bitsLength - n);

                if (coreLength < AllocationThreshold)
                {
                    SquareFinal(valueHigh, valueLow,
                                ZeroMem(stackalloc uint[foldLength]), ZeroMem(stackalloc uint[coreLength]),
                                bitsHigh, bitsLow,
                                result);
                }
                else
                {
                    SquareFinal(valueHigh, valueLow,
                                new uint[foldLength], new uint[coreLength],
                                bitsHigh, bitsLow,
                                result);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static void SquareFinal(Span<uint> valueHigh, Span<uint> valueLow,
                                        Span<uint> fold, Span<uint> core,
                                        Span<uint> bitsHigh, Span<uint> bitsLow,
                                        Span<uint> result)
                {
                    // ... compute z_a = a_1 + a_0 (call it fold...)
                    Add(ref GetReference(valueHigh), valueHigh.Length,
                        ref GetReference(valueLow), valueLow.Length,
                        ref GetReference(fold), fold.Length);

                    // ... compute z_1 = z_a * z_a - z_0 - z_2
                    Square(ref GetReference(fold), fold.Length,
                            ref GetReference(core), core.Length);
                    SubtractCore(ref GetReference(bitsHigh), bitsHigh.Length,
                                    ref GetReference(bitsLow), bitsLow.Length,
                                    ref GetReference(core), core.Length);

                    // ... and finally merge the result! :-)
                    AddSelf(ref GetReference(result), result.Length, ref GetReference(core), core.Length);
                }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Span<uint> ZeroMem(Span<uint> memory)
        {
            memory.Clear();
            return memory;
        }

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
                Span<uint> leftLow = CreateSpan(ref left, n);
                Span<uint> leftHigh = CreateSpan(ref Unsafe.Add(ref left, n), leftLength - n);

                // ... split right like b = (b_1 << n) + b_0
                Span<uint> rightLow = CreateSpan(ref right, n);
                Span<uint> rightHigh = CreateSpan(ref Unsafe.Add(ref right, n), rightLength - n);

                // ... prepare our result array (to reuse its memory)
                Span<uint> bitsLow = CreateSpan(ref bits, n2);
                Span<uint> bitsHigh = CreateSpan(ref Unsafe.Add(ref bits, n2), bitsLength - n2);

                // ... compute z_0 = a_0 * b_0 (multiply again)
                Multiply(ref GetReference(leftLow), leftLow.Length,
                         ref GetReference(rightLow), rightLow.Length,
                         ref GetReference(bitsLow), bitsLow.Length);

                // ... compute z_2 = a_1 * b_1 (multiply again)
                Multiply(ref GetReference(leftHigh), leftHigh.Length,
                         ref GetReference(rightHigh), rightHigh.Length,
                         ref GetReference(bitsHigh), bitsHigh.Length);

                int leftFoldLength = leftHigh.Length + 1;
                int rightFoldLength = rightHigh.Length + 1;
                int coreLength = leftFoldLength + rightFoldLength;

                Span<uint> result = CreateSpan(ref Unsafe.Add(ref bits, n), bitsLength - n);

                if (coreLength < AllocationThreshold)
                {
                    MultiplyFinal(leftHigh, leftLow, ZeroMem(stackalloc uint[leftFoldLength]),
                                    rightHigh, rightLow, ZeroMem(stackalloc uint[rightFoldLength]),
                                    bitsHigh, bitsLow, ZeroMem(stackalloc uint[coreLength]),
                                    result);
                }
                else
                {
                    MultiplyFinal(leftHigh, leftLow, new uint[leftFoldLength],
                                    rightHigh, rightLow, new uint[rightFoldLength],
                                    bitsHigh, bitsLow, new uint[coreLength],
                                    result);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static void MultiplyFinal(Span<uint> leftHigh, Span<uint> leftLow, Span<uint> leftFold,
                                            Span<uint> rightHigh, Span<uint> rightLow, Span<uint> rightFold,
                                            Span<uint> bitsHigh, Span<uint> bitsLow, Span<uint> core,
                                            Span<uint> result)
                {
                    // ... compute z_a = a_1 + a_0 (call it fold...)
                    Add(ref GetReference(leftHigh), leftHigh.Length,
                        ref GetReference(leftLow), leftLow.Length,
                        ref GetReference(leftFold), leftFold.Length);

                    // ... compute z_b = b_1 + b_0 (call it fold...)
                    Add(ref GetReference(rightHigh), rightHigh.Length,
                        ref GetReference(rightLow), rightLow.Length,
                        ref GetReference(rightFold), rightFold.Length);

                    // ... compute z_1 = z_a * z_b - z_0 - z_2
                    Multiply(ref GetReference(leftFold), leftFold.Length,
                                ref GetReference(rightFold), rightFold.Length,
                                ref GetReference(core), core.Length);
                    SubtractCore(ref GetReference(bitsHigh), bitsHigh.Length,
                                    ref GetReference(bitsLow), bitsLow.Length,
                                    ref GetReference(core), core.Length);

                    // ... and finally merge the result! :-)
                    AddSelf(ref GetReference(result), result.Length, ref GetReference(core), core.Length);
                }
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
