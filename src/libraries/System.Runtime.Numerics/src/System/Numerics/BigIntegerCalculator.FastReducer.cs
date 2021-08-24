// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        // If we need to reduce by a certain modulus again and again, it's much
        // more efficient to do this with multiplication operations. This is
        // possible, if we do some pre-computations first...

        // see https://en.wikipedia.org/wiki/Barrett_reduction

        private readonly ref struct FastReducer
        {
            private readonly ReadOnlySpan<uint> _modulus;
            private readonly ReadOnlySpan<uint> _mu;
            private readonly Span<uint> _q1;
            private readonly Span<uint> _q2;

            public FastReducer(ReadOnlySpan<uint> modulus, Span<uint> r, Span<uint> mu, Span<uint> q1, Span<uint> q2)
            {
                Debug.Assert(!modulus.IsEmpty);
                Debug.Assert(r.Length == modulus.Length * 2 + 1);
                Debug.Assert(mu.Length == r.Length - modulus.Length + 1);
                Debug.Assert(q1.Length == modulus.Length * 2 + 2);
                Debug.Assert(q2.Length == modulus.Length * 2 + 2);

                // Let r = 4^k, with 2^k > m
                r[r.Length - 1] = 1;

                // Let mu = 4^k / m
                Divide(r, modulus, mu);
                _modulus = modulus;

                _q1 = q1;
                _q2 = q2;

                _mu = mu.Slice(0, ActualLength(mu));
            }

            public int Reduce(Span<uint> value)
            {
                Debug.Assert(value.Length <= _modulus.Length * 2);

                // Trivial: value is shorter
                if (value.Length < _modulus.Length)
                    return value.Length;

                // Let q1 = v/2^(k-1) * mu
                _q1.Clear();
                int l1 = DivMul(value, _mu, _q1, _modulus.Length - 1);

                // Let q2 = q1/2^(k+1) * m
                _q2.Clear();
                int l2 = DivMul(_q1.Slice(0, l1), _modulus, _q2, _modulus.Length + 1);

                // Let v = (v - q2) % 2^(k+1) - i*m
                var length = SubMod(value, _q2.Slice(0, l2), _modulus, _modulus.Length + 1);
                value = value.Slice(length);
                value.Clear();

                return length;
            }

            private static int DivMul(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits, int k)
            {
                Debug.Assert(!right.IsEmpty);
                Debug.Assert(!bits.IsEmpty);
                Debug.Assert(bits.Length + k >= left.Length + right.Length);

                // Executes the multiplication algorithm for left and right,
                // but skips the first k limbs of left, which is equivalent to
                // preceding division by 2^(32*k). To spare memory allocations
                // we write the result to an already allocated memory.

                if (left.Length > k)
                {
                    left = left.Slice(k);

                    if (left.Length < right.Length)
                    {
                        Multiply(right,
                                 left,
                                 bits.Slice(0, left.Length + right.Length));
                    }
                    else
                    {
                        Multiply(left,
                                 right,
                                 bits.Slice(0, left.Length + right.Length));
                    }

                    return ActualLength(bits.Slice(0, left.Length + right.Length));
                }

                return 0;
            }

            private static int SubMod(Span<uint> left, ReadOnlySpan<uint> right, ReadOnlySpan<uint> modulus, int k)
            {
                // Executes the subtraction algorithm for left and right,
                // but considers only the first k limbs, which is equivalent to
                // preceding reduction by 2^(32*k). Furthermore, if left is
                // still greater than modulus, further subtractions are used.

                if (left.Length > k)
                    left = left.Slice(0, k);
                if (right.Length > k)
                    right = right.Slice(0, k);

                SubtractSelf(left, right);
                left = left.Slice(0, ActualLength(left));

                while (Compare(left, modulus) >= 0)
                {
                    SubtractSelf(left, modulus);
                    left = left.Slice(0, ActualLength(left));
                }

                return left.Length;
            }
        }
    }
}
