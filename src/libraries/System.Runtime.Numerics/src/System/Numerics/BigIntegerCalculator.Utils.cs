// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        private static int Reduce(Span<nuint> bits, ReadOnlySpan<nuint> modulus)
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
        public static void InitializeForDebug(Span<nuint> bits)
        {
            // Reproduce the case where the return value of `stackalloc nuint` is not initialized to zero.
            bits.Fill(0xCD);
        }
    }
}
