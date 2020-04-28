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

            Square(value, bits);

            return bits;
        }

        // Mutable for unit testing...
        private static int SquareThreshold = 32;
        private static int AllocationThreshold = 256;

        private static void Square(ReadOnlySpan<uint> value, Span<uint> bits)
        {
            Debug.Assert(value.Length >= 0);
            Debug.Assert(bits.Length == value.Length + value.Length);

            // Executes different algorithms for computing z = a * a
            // based on the actual length of a. If a is "small" enough
            // we stick to the classic "grammar-school" method; for the
            // rest we switch to implementations with less complexity
            // albeit more overhead (which needs to pay off!).

            // NOTE: useful thresholds needs some "empirical" testing,
            // which are smaller in DEBUG mode for testing purpose.

            if (value.Length < SquareThreshold)
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
                ref uint elementPtr = ref NullRef;
                for (int i = 0; i < value.Length; i++)
                {
                    ulong carry = 0UL;
                    for (int j = 0; j < i; j++)
                    {
                        elementPtr = ref Unsafe.Add(ref GetReference(bits), i + j);
                        ulong digit1 = elementPtr + carry;
                        ulong digit2 = (ulong)Unsafe.Add(ref GetReference(value), j) * Unsafe.Add(ref GetReference(value), i);
                        elementPtr = unchecked((uint)(digit1 + (digit2 << 1)));
                        carry = (digit2 + (digit1 >> 1)) >> 31;
                    }
                    elementPtr = ref Unsafe.Add(ref GetReference(value), i);
                    ulong digits = (ulong)elementPtr * elementPtr + carry;
                    elementPtr = ref Unsafe.Add(ref GetReference(bits), i + i);
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
                int n = value.Length >> 1;
                int n2 = n << 1;

                // ... split value like a = (a_1 << n) + a_0
                ReadOnlySpan<uint> valueLow = value.Slice(0, n);
                ReadOnlySpan<uint> valueHigh = value.Slice(n);

                // ... prepare our result array (to reuse its memory)
                Span<uint> bitsLow = bits.Slice(0, n2);
                Span<uint> bitsHigh = bits.Slice(n2);

                // ... compute z_0 = a_0 * a_0 (squaring again!)
                Square(valueLow, bitsLow);

                // ... compute z_2 = a_1 * a_1 (squaring again!)
                Square(valueHigh, bitsHigh);

                int foldLength = valueHigh.Length + 1;
                int coreLength = foldLength + foldLength;

                Span<uint> result = bits.Slice(n);

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
                static void SquareFinal(ReadOnlySpan<uint> valueHigh, ReadOnlySpan<uint> valueLow,
                                        Span<uint> fold, Span<uint> core,
                                        ReadOnlySpan<uint> bitsHigh, ReadOnlySpan<uint> bitsLow,
                                        Span<uint> result)
                {
                    // ... compute z_a = a_1 + a_0 (call it fold...)
                    Add(valueHigh, valueLow, fold);

                    // ... compute z_1 = z_a * z_a - z_0 - z_2
                    Square(fold, core);

                    SubtractCore(bitsHigh, bitsLow, core);

                    // ... and finally merge the result! :-)
                    AddSelf(result, core);
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

            Multiply(left, right, bits);

            return bits;
        }

        // Mutable for unit testing...
        private static int MultiplyThreshold = 32;

        private static void Multiply(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits)
        {
            Debug.Assert(left.Length >= 0);
            Debug.Assert(right.Length >= 0);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(bits.Length == left.Length + right.Length);

            // Executes different algorithms for computing z = a * b
            // based on the actual length of b. If b is "small" enough
            // we stick to the classic "grammar-school" method; for the
            // rest we switch to implementations with less complexity
            // albeit more overhead (which needs to pay off!).

            // NOTE: useful thresholds needs some "empirical" testing,
            // which are smaller in DEBUG mode for testing purpose.

            if (right.Length < MultiplyThreshold)
            {
                // Multiplies the bits using the "grammar-school" method.
                // Envisioning the "rhombus" of a pen-and-paper calculation
                // should help getting the idea of these two loops...
                // The inner multiplication operations are safe, because
                // z_i+j + a_j * b_i + c <= 2(2^32 - 1) + (2^32 - 1)^2 =
                // = 2^64 - 1 (which perfectly matches with ulong!).

                ref uint elementPtr = ref NullRef;
                for (int i = 0; i < right.Length; i++)
                {
                    ulong carry = 0UL;
                    for (int j = 0; j < left.Length; j++)
                    {
                        elementPtr = ref Unsafe.Add(ref GetReference(bits), i + j);
                        ulong digits = elementPtr + carry
                            + (ulong)Unsafe.Add(ref GetReference(left), j) * Unsafe.Add(ref GetReference(right), i);
                        elementPtr = unchecked((uint)digits);
                        carry = digits >> 32;
                    }
                    Unsafe.Add(ref GetReference(bits), i + left.Length) = (uint)carry;
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
                int n = right.Length >> 1;
                int n2 = n << 1;

                // ... split left like a = (a_1 << n) + a_0
                ReadOnlySpan<uint> leftLow = left.Slice(0, n);
                ReadOnlySpan<uint> leftHigh = left.Slice(n);

                // ... split right like b = (b_1 << n) + b_0
                ReadOnlySpan<uint> rightLow = right.Slice(0, n);
                ReadOnlySpan<uint> rightHigh = right.Slice(n);

                // ... prepare our result array (to reuse its memory)
                Span<uint> bitsLow = bits.Slice(0, n2);
                Span<uint> bitsHigh = bits.Slice(n2);

                // ... compute z_0 = a_0 * b_0 (multiply again)
                Multiply(leftLow, rightLow, bitsLow);

                // ... compute z_2 = a_1 * b_1 (multiply again)
                Multiply(leftHigh, rightHigh, bitsHigh);

                int leftFoldLength = leftHigh.Length + 1;
                int rightFoldLength = rightHigh.Length + 1;
                int coreLength = leftFoldLength + rightFoldLength;

                Span<uint> result = bits.Slice(n);

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
                static void MultiplyFinal(ReadOnlySpan<uint> leftHigh, ReadOnlySpan<uint> leftLow, Span<uint> leftFold,
                                            ReadOnlySpan<uint> rightHigh, ReadOnlySpan<uint> rightLow, Span<uint> rightFold,
                                            ReadOnlySpan<uint> bitsHigh, ReadOnlySpan<uint> bitsLow, Span<uint> core,
                                            Span<uint> result)
                {
                    // ... compute z_a = a_1 + a_0 (call it fold...)
                    Add(leftHigh, leftLow, leftFold);

                    // ... compute z_b = b_1 + b_0 (call it fold...)
                    Add(rightHigh, rightLow, rightFold);

                    // ... compute z_1 = z_a * z_b - z_0 - z_2
                    Multiply(leftFold, rightFold, core);
                    SubtractCore(bitsHigh, bitsLow, core);

                    // ... and finally merge the result! :-)
                    AddSelf(result, core);
                }
            }
        }

        private static void SubtractCore(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> core)
        {
            Debug.Assert(left.Length >= 0);
            Debug.Assert(right.Length >= 0);
            Debug.Assert(core.Length >= 0);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(core.Length >= left.Length);

            // Executes a special subtraction algorithm for the multiplication,
            // which needs to subtract two different values from a core value,
            // while core is always bigger than the sum of these values.

            // NOTE: we could do an ordinary subtraction of course, but we spare
            // one "run", if we do this computation within a single one...

            int i = 0;
            long carry = 0L;

            ref uint elementPtr = ref NullRef;
            for (; i < right.Length; i++)
            {
                elementPtr = ref Unsafe.Add(ref GetReference(core), i);
                long digit = (elementPtr + carry) - Unsafe.Add(ref GetReference(left), i) - Unsafe.Add(ref GetReference(right), i);
                elementPtr = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for (; i < left.Length; i++)
            {
                elementPtr = ref Unsafe.Add(ref GetReference(core), i);
                long digit = (elementPtr + carry) - Unsafe.Add(ref GetReference(left), i);
                elementPtr = unchecked((uint)digit);
                carry = digit >> 32;
            }
            for (; carry != 0 && i < core.Length; i++)
            {
                elementPtr = ref Unsafe.Add(ref GetReference(core), i);
                long digit = elementPtr + carry;
                elementPtr = (uint)digit;
                carry = digit >> 32;
            }
        }
    }
}
