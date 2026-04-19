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
            private readonly ReadOnlySpan<nuint> _modulus;
            private readonly ReadOnlySpan<nuint> _mu;
            private readonly Span<nuint> _q1;
            private readonly Span<nuint> _q2;

            public FastReducer(ReadOnlySpan<nuint> modulus, Span<nuint> r, Span<nuint> mu, Span<nuint> q1, Span<nuint> q2)
            {
                Debug.Assert(!modulus.IsEmpty);
                Debug.Assert(r.Length == modulus.Length * 2 + 1);
                Debug.Assert(mu.Length == r.Length - modulus.Length + 1);
                Debug.Assert(q1.Length == modulus.Length * 2 + 2);
                Debug.Assert(q2.Length == modulus.Length * 2 + 2);

                // Barrett reduction: precompute mu = floor(4^k / m), where k = modulus.Length.
                // Start by setting r = 4^k (a 1 in the highest position of a 2k+1 limb number).
                r[^1] = 1;

                // Compute mu = floor(r / m)
                DivRem(r, modulus, mu);
                _modulus = modulus;

                _q1 = q1;
                _q2 = q2;

                _mu = mu.Slice(0, ActualLength(mu));
            }

            public int Reduce(Span<nuint> value)
            {
                Debug.Assert(value.Length <= _modulus.Length * 2);

                // Trivial: value is shorter
                if (value.Length < _modulus.Length)
                {
                    return value.Length;
                }

                // Let q1 = v/2^(k-1) * mu
                _q1.Clear();
                int l1 = DivMul(value, _mu, _q1, _modulus.Length - 1);

                // Let q2 = q1/2^(k+1) * m
                _q2.Clear();
                int l2 = DivMul(_q1.Slice(0, l1), _modulus, _q2, _modulus.Length + 1);

                // Let v = (v - q2) % 2^(k+1) - i*m
                int length = SubMod(value, _q2.Slice(0, l2), _modulus, _modulus.Length + 1);
                value = value.Slice(length);
                value.Clear();

                return length;
            }

            private static int DivMul(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits, int k)
            {
                Debug.Assert(!right.IsEmpty);
                Debug.Assert(!bits.IsEmpty);
                Debug.Assert(bits.Length + k >= left.Length + right.Length);

                // Executes the multiplication algorithm for left and right,
                // but skips the first k limbs of left, which is equivalent to
                // preceding division by 2^(BitsPerLimb*k). To spare memory allocations
                // we write the result to an already allocated memory.

                if (left.Length > k)
                {
                    left = left.Slice(k);
                    bits = bits.Slice(0, left.Length + right.Length);

                    Multiply(left, right, bits);

                    return ActualLength(bits);
                }

                return 0;
            }

            private static int SubMod(Span<nuint> left, ReadOnlySpan<nuint> right, ReadOnlySpan<nuint> modulus, int k)
            {
                // Executes the subtraction algorithm for left and right,
                // but considers only the first k limbs, which is equivalent to
                // preceding reduction by 2^(BitsPerLimb*k). Furthermore, if left is
                // still greater than modulus, further subtractions are used.

                if (left.Length > k)
                {
                    left = left.Slice(0, k);
                }

                if (right.Length > k)
                {
                    right = right.Slice(0, k);
                }

                // Barrett reduction guarantees the true residual x - q̂·m is in
                // [0, 2·modulus], but after truncating both sides to k limbs the
                // truncated right can appear larger than the truncated left.
                // Unsigned underflow is safe here: the wrapped result equals the
                // true residual, which is guaranteed to fit in k limbs because
                // 2·modulus < b^k where b = 2^BitsPerLimb.
                Debug.Assert(left.Length >= right.Length);
                {
                    int i = 0;
                    nuint borrow = 0;

                    if (right.Length != 0)
                    {
                        _ = left[right.Length - 1];
                    }

                    for (; i < right.Length; i++)
                    {
                        left[i] = SubWithBorrow(left[i], right[i], borrow, out borrow);
                    }

                    for (; borrow != 0 && i < left.Length; i++)
                    {
                        nuint val = left[i];
                        left[i] = val - borrow;
                        borrow = val == 0 ? 1 : (nuint)0;
                    }
                }

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
