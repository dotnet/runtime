// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

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

        public static int Compare(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
        {
            if (left.Length < right.Length)
                return -1;
            if (left.Length > right.Length)
                return 1;

            for (int i = left.Length - 1; i >= 0; i--)
            {
                uint leftElement = left[i];
                uint rightElement = right[i];
                if (leftElement < rightElement)
                    return -1;
                if (leftElement > rightElement)
                    return 1;
            }

            return 0;
        }

        private static int CompareActual(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
        {
            if (left.Length != right.Length)
            {
                if (left.Length < right.Length)
                {
                    if (right.Slice(left.Length).ContainsAnyExcept(0u))
                        return -1;
                    right = right.Slice(0, left.Length);
                }
                else
                {
                    if (left.Slice(right.Length).ContainsAnyExcept(0u))
                        return +1;
                    left = left.Slice(0, right.Length);
                }
            }
            return Compare(left, right);
        }

        private static int ActualLength(ReadOnlySpan<uint> value)
        {
            // Since we're reusing memory here, the actual length
            // of a given value may be less then the array's length

            int length = value.Length;

            while (length > 0 && value[length - 1] == 0)
                --length;
            return length;
        }

        private static int Reduce(Span<uint> bits, ReadOnlySpan<uint> modulus)
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
        public static void DummyForDebug(Span<uint> bits)
        {
            // Reproduce the case where the return value of `stackalloc uint` is not initialized to zero.
            bits.Fill(0xCD);
        }
    }
}
