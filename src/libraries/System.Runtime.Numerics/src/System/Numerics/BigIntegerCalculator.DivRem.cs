// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
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
        int DivideThreshold = 32;

        public static void Divide(ReadOnlySpan<uint> left, uint right, Span<uint> quotient, out uint remainder)
        {
            DummyForDebug(quotient);
            ulong carry = 0UL;
            Divide(left, right, quotient, ref carry);
            remainder = (uint)carry;
        }

        public static void Divide(ReadOnlySpan<uint> left, uint right, Span<uint> quotient)
        {
            DummyForDebug(quotient);
            ulong carry = 0UL;
            Divide(left, right, quotient, ref carry);
        }

        private static void Divide(ReadOnlySpan<uint> left, uint right, Span<uint> quotient, ref ulong carry)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(quotient.Length == left.Length);
            DummyForDebug(quotient);

            // Executes the division for one big and one 32-bit integer.
            // Thus, we've similar code than below, but there is no loop for
            // processing the 32-bit integer, since it's a single element.

            for (int i = left.Length - 1; i >= 0; i--)
            {
                ulong value = (carry << 32) | left[i];
                ulong digit = value / right;
                quotient[i] = (uint)digit;
                carry = value - digit * right;
            }
        }

        public static uint Remainder(ReadOnlySpan<uint> left, uint right)
        {
            Debug.Assert(left.Length >= 1);

            // Same as above, but only computing the remainder.
            ulong carry = 0UL;
            for (int i = left.Length - 1; i >= 0; i--)
            {
                ulong value = (carry << 32) | left[i];
                carry = value % right;
            }

            return (uint)carry;
        }

        public static void Divide(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> quotient, Span<uint> remainder)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(quotient.Length == left.Length - right.Length + 1);
            Debug.Assert(remainder.Length == left.Length);
            DummyForDebug(quotient);
            DummyForDebug(remainder);

            if (right.Length < DivideThreshold || left.Length - right.Length < DivideThreshold)
            {
                left.CopyTo(remainder);
                DivideGrammarSchool(remainder, right, quotient);
            }
            else
                DivideBurnikelZiegler(left, right, quotient, remainder);
        }

        public static void Divide(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> quotient)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(quotient.Length == left.Length - right.Length + 1);
            DummyForDebug(quotient);

            if (right.Length < DivideThreshold || left.Length - right.Length < DivideThreshold)
            {
                // Same as above, but only returning the quotient.

                uint[]? leftCopyFromPool = null;

                // NOTE: left will get overwritten, we need a local copy
                // However, mutated left is not used afterwards, so use array pooling or stack alloc
                Span<uint> leftCopy = (left.Length <= StackAllocThreshold ?
                                      stackalloc uint[StackAllocThreshold]
                                      : leftCopyFromPool = ArrayPool<uint>.Shared.Rent(left.Length)).Slice(0, left.Length);
                left.CopyTo(leftCopy);

                DivideGrammarSchool(leftCopy, right, quotient);

                if (leftCopyFromPool != null)
                    ArrayPool<uint>.Shared.Return(leftCopyFromPool);
            }
            else
                DivideBurnikelZiegler(left, right, quotient, default);

        }

        public static void Remainder(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> remainder)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(remainder.Length == left.Length);
            DummyForDebug(remainder);

            if (right.Length < DivideThreshold || left.Length - right.Length < DivideThreshold)
            {
                // Same as above, but only returning the remainder.

                left.CopyTo(remainder);
                DivideGrammarSchool(remainder, right, default);
            }
            else
            {
                int quotientLength = left.Length - right.Length + 1;
                uint[]? quotientFromPool = null;

                Span<uint> quotient = (quotientLength <= StackAllocThreshold ?
                                      stackalloc uint[StackAllocThreshold]
                                      : quotientFromPool = ArrayPool<uint>.Shared.Rent(quotientLength)).Slice(0, quotientLength);

                DivideBurnikelZiegler(left, right, quotient, remainder);

                if (quotientFromPool != null)
                    ArrayPool<uint>.Shared.Return(quotientFromPool);
            }
        }

        private static void DivRem(Span<uint> left, ReadOnlySpan<uint> right, Span<uint> quotient)
        {
            // quotient = left / right;
            // left %= right;

            Debug.Assert(left.Length >= 1);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(quotient.Length == left.Length - right.Length + 1
                || quotient.Length == 0);
            DummyForDebug(quotient);

            if (right.Length < DivideThreshold || left.Length - right.Length < DivideThreshold)
                DivideGrammarSchool(left, right, quotient);
            else
            {
                uint[]? leftCopyFromPool = null;

                // NOTE: left will get overwritten, we need a local copy
                // However, mutated left is not used afterwards, so use array pooling or stack alloc
                Span<uint> leftCopy = (left.Length <= StackAllocThreshold ?
                                      stackalloc uint[StackAllocThreshold]
                                      : leftCopyFromPool = ArrayPool<uint>.Shared.Rent(left.Length)).Slice(0, left.Length);
                left.CopyTo(leftCopy);

                uint[]? quotientActualFromPool = null;
                scoped Span<uint> quotientActual;

                if (quotient.Length == 0)
                {
                    int quotientLength = left.Length - right.Length + 1;

                    quotientActual = (quotientLength <= StackAllocThreshold ?
                                stackalloc uint[StackAllocThreshold]
                                : quotientActualFromPool = ArrayPool<uint>.Shared.Rent(quotientLength)).Slice(0, quotientLength);
                }
                else
                    quotientActual = quotient;

                DivideBurnikelZiegler(leftCopy, right, quotientActual, left);

                if (quotientActualFromPool != null)
                    ArrayPool<uint>.Shared.Return(quotientActualFromPool);
                if (leftCopyFromPool != null)
                    ArrayPool<uint>.Shared.Return(leftCopyFromPool);
            }
        }

        private static void DivideGrammarSchool(Span<uint> left, ReadOnlySpan<uint> right, Span<uint> quotient)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(
                quotient.Length == 0
                || quotient.Length == left.Length - right.Length + 1
                || (CompareActual(left.Slice(left.Length - right.Length), right) < 0 && quotient.Length == left.Length - right.Length));

            // Executes the "grammar-school" algorithm for computing q = a / b.
            // Before calculating q_i, we get more bits into the highest bit
            // block of the divisor. Thus, guessing digits of the quotient
            // will be more precise. Additionally we'll get r = a % b.

            uint divHi = right[right.Length - 1];
            uint divLo = right.Length > 1 ? right[right.Length - 2] : 0;

            // We measure the leading zeros of the divisor
            int shift = BitOperations.LeadingZeroCount(divHi);
            int backShift = 32 - shift;

            // And, we make sure the most significant bit is set
            if (shift > 0)
            {
                uint divNx = right.Length > 2 ? right[right.Length - 3] : 0;

                divHi = (divHi << shift) | (divLo >> backShift);
                divLo = (divLo << shift) | (divNx >> backShift);
            }

            // Then, we divide all of the bits as we would do it using
            // pen and paper: guessing the next digit, subtracting, ...
            for (int i = left.Length; i >= right.Length; i--)
            {
                int n = i - right.Length;
                uint t = (uint)i < (uint)left.Length ? left[i] : 0;

                ulong valHi = ((ulong)t << 32) | left[i - 1];
                uint valLo = i > 1 ? left[i - 2] : 0;

                // We shifted the divisor, we shift the dividend too
                if (shift > 0)
                {
                    uint valNx = i > 2 ? left[i - 3] : 0;

                    valHi = (valHi << shift) | (valLo >> backShift);
                    valLo = (valLo << shift) | (valNx >> backShift);
                }

                // First guess for the current digit of the quotient,
                // which naturally must have only 32 bits...
                ulong digit = valHi / divHi;
                if (digit > 0xFFFFFFFF)
                    digit = 0xFFFFFFFF;

                // Our first guess may be a little bit to big
                while (DivideGuessTooBig(digit, valHi, valLo, divHi, divLo))
                    --digit;

                if (digit > 0)
                {
                    // Now it's time to subtract our current quotient
                    uint carry = SubtractDivisor(left.Slice(n), right, digit);
                    if (carry != t)
                    {
                        Debug.Assert(carry == t + 1);

                        // Our guess was still exactly one too high
                        carry = AddDivisor(left.Slice(n), right);
                        --digit;

                        Debug.Assert(carry == 1);
                    }
                }

                // We have the digit!
                if ((uint)n < (uint)quotient.Length)
                    quotient[n] = (uint)digit;

                if ((uint)i < (uint)left.Length)
                    left[i] = 0;
            }
        }

        private static uint AddDivisor(Span<uint> left, ReadOnlySpan<uint> right)
        {
            Debug.Assert(left.Length >= right.Length);

            // Repairs the dividend, if the last subtract was too much

            ulong carry = 0UL;

            for (int i = 0; i < right.Length; i++)
            {
                ref uint leftElement = ref left[i];
                ulong digit = (leftElement + carry) + right[i];
                leftElement = unchecked((uint)digit);
                carry = digit >> 32;
            }

            return (uint)carry;
        }

        private static uint SubtractDivisor(Span<uint> left, ReadOnlySpan<uint> right, ulong q)
        {
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(q <= 0xFFFFFFFF);

            // Combines a subtract and a multiply operation, which is naturally
            // more efficient than multiplying and then subtracting...

            ulong carry = 0UL;

            for (int i = 0; i < right.Length; i++)
            {
                carry += right[i] * q;
                uint digit = unchecked((uint)carry);
                carry >>= 32;
                ref uint leftElement = ref left[i];
                if (leftElement < digit)
                    ++carry;
                leftElement -= digit;
            }

            return (uint)carry;
        }

        private static bool DivideGuessTooBig(ulong q, ulong valHi, uint valLo,
                                              uint divHi, uint divLo)
        {
            Debug.Assert(q <= 0xFFFFFFFF);

            // We multiply the two most significant limbs of the divisor
            // with the current guess for the quotient. If those are bigger
            // than the three most significant limbs of the current dividend
            // we return true, which means the current guess is still too big.

            ulong chkHi = divHi * q;
            ulong chkLo = divLo * q;

            chkHi += (chkLo >> 32);
            uint chkLoUInt32 = (uint)(chkLo);

            return (chkHi > valHi) || ((chkHi == valHi) && (chkLoUInt32 > valLo));
        }

        private static void DivideBurnikelZiegler(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> quotient, Span<uint> remainder)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(quotient.Length == left.Length - right.Length + 1);
            Debug.Assert(remainder.Length == left.Length
                        || remainder.Length == 0);

            // Executes the Burnikel-Ziegler algorithm for computing q = a / b.
            //
            // Burnikel, C., Ziegler, J.: Fast recursive division. Research Report MPI-I-98-1-022, MPI Saarbrücken, 1998


            // Fast recursive division: Algorithm 3
            int n;
            {
                int m = (int)BitOperations.RoundUpToPowerOf2((uint)(right.Length + DivideThreshold - 1) / (uint)DivideThreshold);

                int j = (right.Length + m - 1) / m; // Ceil(right.Length/m)
                n = j * m;
            }

            int sigmaDigit = n - right.Length;
            int sigmaSmall = BitOperations.LeadingZeroCount(right[^1]);

            uint[]? bFromPool = null;

            Span<uint> b = (n <= StackAllocThreshold ?
                            stackalloc uint[StackAllocThreshold]
                            : bFromPool = ArrayPool<uint>.Shared.Rent(n)).Slice(0, n);

            int aLength = left.Length + sigmaDigit;

            // if: BitOperations.LeadingZeroCount(left[^1]) < sigmaSmall, requires one more digit obviously.
            // if: BitOperations.LeadingZeroCount(left[^1]) == sigmaSmall, requires one more digit, because the leftmost bit of a must be 0.

            if (BitOperations.LeadingZeroCount(left[^1]) <= sigmaSmall)
                ++aLength;

            uint[]? aFromPool = null;

            Span<uint> a = (aLength <= StackAllocThreshold ?
                            stackalloc uint[StackAllocThreshold]
                            : aFromPool = ArrayPool<uint>.Shared.Rent(aLength)).Slice(0, aLength);

            // 4. normalize
            static void Normalize(ReadOnlySpan<uint> src, int sigmaDigit, int sigmaSmall, Span<uint> bits)
            {
                Debug.Assert((uint)sigmaSmall <= 32);
                Debug.Assert(src.Length + sigmaDigit <= bits.Length);

                bits.Slice(0, sigmaDigit).Clear();
                Span<uint> dst = bits.Slice(sigmaDigit);
                src.CopyTo(dst);
                dst.Slice(src.Length).Clear();

                if (sigmaSmall != 0)
                {
                    // Left shift
                    int carryShift = 32 - sigmaSmall;
                    uint carry = 0;
                    for (int i = 0; i < bits.Length; i++)
                    {
                        uint carryTmp = bits[i] >> carryShift;
                        bits[i] = bits[i] << sigmaSmall | carry;
                        carry = carryTmp;
                    }
                    Debug.Assert(carry == 0);
                }
            }

            Normalize(left, sigmaDigit, sigmaSmall, a);
            Normalize(right, sigmaDigit, sigmaSmall, b);


            int t = Math.Max(2, (a.Length + n - 1) / n); // Max(2, Ceil(a.Length/n))
            Debug.Assert(t < a.Length || (t == a.Length && (int)a[^1] >= 0));

            uint[]? rFromPool = null;
            Span<uint> r = ((n + 1) <= StackAllocThreshold ?
                            stackalloc uint[StackAllocThreshold]
                            : rFromPool = ArrayPool<uint>.Shared.Rent(n + 1)).Slice(0, n + 1);

            uint[]? zFromPool = null;
            Span<uint> z = (2 * n <= StackAllocThreshold ?
                            stackalloc uint[StackAllocThreshold]
                            : zFromPool = ArrayPool<uint>.Shared.Rent(2 * n)).Slice(0, 2 * n);
            a.Slice((t - 2) * n).CopyTo(z);
            z.Slice(a.Length - (t - 2) * n).Clear();

            Span<uint> quotientUpper = quotient.Slice((t - 2) * n);
            if (quotientUpper.Length < n)
            {
                uint[]? qFromPool = null;
                Span<uint> q = (n <= StackAllocThreshold ?
                                stackalloc uint[StackAllocThreshold]
                                : qFromPool = ArrayPool<uint>.Shared.Rent(n)).Slice(0, n);

                BurnikelZieglerD2n1n(z, b, q, r);

                Debug.Assert(!q.Slice(quotientUpper.Length).ContainsAnyExcept(0u));
                q.Slice(0, quotientUpper.Length).CopyTo(quotientUpper);

                if (qFromPool != null)
                    ArrayPool<uint>.Shared.Return(qFromPool);
            }
            else
            {
                BurnikelZieglerD2n1n(z, b, quotientUpper.Slice(0, n), r);
                quotientUpper.Slice(n).Clear();
            }

            for (int i = t - 3; i >= 0; i--)
            {
                a.Slice(i * n, n).CopyTo(z);
                r.Slice(0, n).CopyTo(z.Slice(n));
                BurnikelZieglerD2n1n(z, b, quotient.Slice(i * n, n), r);
            }

            if (zFromPool != null)
                ArrayPool<uint>.Shared.Return(zFromPool);
            if (bFromPool != null)
                ArrayPool<uint>.Shared.Return(bFromPool);
            if (aFromPool != null)
                ArrayPool<uint>.Shared.Return(aFromPool);

            Debug.Assert(r[^1] == 0);
            Debug.Assert(!r.Slice(0, sigmaDigit).ContainsAnyExcept(0u));
            if (remainder.Length != 0)
            {
                Span<uint> rt = r.Slice(sigmaDigit);
                remainder.Slice(rt.Length).Clear();

                if (sigmaSmall != 0)
                {
                    // Right shift
                    Debug.Assert((uint)sigmaSmall <= 32);
                    int carryShift = 32 - sigmaSmall;
                    uint carry = 0;
                    for (int i = rt.Length - 1; i >= 0; i--)
                    {
                        uint carryTmp = rt[i] << carryShift;
                        rt[i] = rt[i] >> sigmaSmall | carry;
                        carry = carryTmp;
                    }
                    Debug.Assert(carry == 0);
                }

                rt.CopyTo(remainder);
            }

            if (rFromPool != null)
                ArrayPool<uint>.Shared.Return(rFromPool);
        }

        private static void BurnikelZieglerFallback(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> quotient, Span<uint> remainder)
        {
            // Fast recursive division: Algorithm 1
            // 1. If n is odd or smaller than some convenient constant

            Debug.Assert(left.Length == 2 * right.Length);
            Debug.Assert(CompareActual(left.Slice(right.Length), right) < 0);
            Debug.Assert(quotient.Length == right.Length);
            Debug.Assert(remainder.Length >= right.Length + 1);
            Debug.Assert(right[^1] > 0);

            left = left.Slice(0, ActualLength(left));

            if (left.Length < right.Length)
            {
                quotient.Clear();
                left.CopyTo(remainder);
                remainder.Slice(left.Length).Clear();
            }
            else if (right.Length == 1)
            {
                ulong carry;

                if (quotient.Length < left.Length)
                {
                    Debug.Assert(quotient.Length + 1 == left.Length);
                    Debug.Assert(left[^1] < right[0]);

                    carry = left[^1];
                    Divide(left.Slice(0, quotient.Length), right[0], quotient, ref carry);
                }
                else
                {
                    carry = 0;
                    quotient.Slice(left.Length).Clear();
                    Divide(left, right[0], quotient, ref carry);
                }

                if (remainder.Length != 0)
                {
                    remainder.Slice(1).Clear();
                    remainder[0] = (uint)carry;
                }
            }
            else
            {
                uint[]? r1FromPool = null;
                Span<uint> r1 = (left.Length <= StackAllocThreshold ?
                                stackalloc uint[StackAllocThreshold]
                                : r1FromPool = ArrayPool<uint>.Shared.Rent(left.Length)).Slice(0, left.Length);

                left.CopyTo(r1);
                int quotientLength = Math.Min(left.Length - right.Length + 1, quotient.Length);

                quotient.Slice(quotientLength).Clear();
                DivideGrammarSchool(r1, right, quotient.Slice(0, quotientLength));

                if (r1.Length < remainder.Length)
                {
                    remainder.Slice(r1.Length).Clear();
                    r1.CopyTo(remainder);
                }
                else
                {
                    Debug.Assert(!r1.Slice(remainder.Length).ContainsAnyExcept(0u));
                    r1.Slice(0, remainder.Length).CopyTo(remainder);
                }

                if (r1FromPool != null)
                    ArrayPool<uint>.Shared.Return(r1FromPool);
            }
        }


        private static void BurnikelZieglerD2n1n(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> quotient, Span<uint> remainder)
        {
            // Fast recursive division: Algorithm 1
            Debug.Assert(left.Length == 2 * right.Length);
            Debug.Assert(CompareActual(left.Slice(right.Length), right) < 0);
            Debug.Assert(quotient.Length == right.Length);
            Debug.Assert(remainder.Length >= right.Length + 1);
            Debug.Assert(right[^1] > 0);

            if (right.Length % 2 != 0 || right.Length < DivideThreshold)
            {
                BurnikelZieglerFallback(left, right, quotient, remainder);
                return;
            }

            int halfN = right.Length >> 1;

            uint[]? r1FromPool = null;
            Span<uint> r1 = ((right.Length + 1) <= StackAllocThreshold ?
                            stackalloc uint[StackAllocThreshold]
                            : r1FromPool = ArrayPool<uint>.Shared.Rent(right.Length + 1)).Slice(0, right.Length + 1);

            BurnikelZieglerD3n2n(left.Slice(right.Length), left.Slice(halfN, halfN), right, quotient.Slice(halfN), r1);
            BurnikelZieglerD3n2n(r1.Slice(0, right.Length), left.Slice(0, halfN), right, quotient.Slice(0, halfN), remainder);

            if (r1FromPool != null)
                ArrayPool<uint>.Shared.Return(r1FromPool);
        }

        private static void BurnikelZieglerD3n2n(ReadOnlySpan<uint> left12, ReadOnlySpan<uint> left3, ReadOnlySpan<uint> right, Span<uint> quotient, Span<uint> remainder)
        {
            // Fast recursive division: Algorithm 2
            Debug.Assert(right.Length % 2 == 0);
            Debug.Assert(left12.Length == right.Length);
            Debug.Assert(2 * left3.Length == right.Length);
            Debug.Assert(2 * quotient.Length == right.Length);
            Debug.Assert(remainder.Length >= right.Length + 1);
            Debug.Assert(right[^1] > 0);

            int halfN = right.Length >> 1;

            ReadOnlySpan<uint> a1 = left12.Slice(halfN);
            ReadOnlySpan<uint> b1 = right.Slice(halfN);
            ReadOnlySpan<uint> b2 = right.Slice(0, halfN);
            Span<uint> r1 = remainder.Slice(halfN);

            if (CompareActual(a1, b1) < 0)
            {
                BurnikelZieglerD2n1n(left12, b1, quotient, r1);
            }
            else
            {
                quotient.Fill(uint.MaxValue);

                uint[]? bbFromPool = null;

                Span<uint> bb = (left12.Length <= StackAllocThreshold ?
                                stackalloc uint[StackAllocThreshold]
                                : bbFromPool = ArrayPool<uint>.Shared.Rent(left12.Length)).Slice(0, left12.Length);
                b1.CopyTo(bb.Slice(halfN));
                r1.Clear();

                SubtractSelf(bb, b1);
                SubtractSelf(r1, bb);

                if (bbFromPool != null)
                    ArrayPool<uint>.Shared.Return(bbFromPool);
            }


            uint[]? dFromPool = null;

            Span<uint> d = (right.Length <= StackAllocThreshold ?
                            stackalloc uint[StackAllocThreshold]
                            : dFromPool = ArrayPool<uint>.Shared.Rent(right.Length)).Slice(0, right.Length);
            d.Clear();

            MultiplyActual(quotient, b2, d);

            // R = [R1, A3]
            left3.CopyTo(remainder.Slice(0, halfN));

            Span<uint> rr = remainder.Slice(0, d.Length + 1);

            while (CompareActual(rr, d) < 0)
            {
                AddSelf(rr, right);
                int qi = -1;
                while (quotient[++qi] == 0) ;
                Debug.Assert((uint)qi < (uint)quotient.Length);
                --quotient[qi];
                quotient.Slice(0, qi).Fill(uint.MaxValue);
            }

            SubtractSelf(rr, d);

            if (dFromPool != null)
                ArrayPool<uint>.Shared.Return(dFromPool);

            static void MultiplyActual(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits)
            {
                Debug.Assert(bits.Length == left.Length + right.Length);

                left = left.Slice(0, ActualLength(left));
                right = right.Slice(0, ActualLength(right));
                bits = bits.Slice(0, left.Length + right.Length);

                if (left.Length < right.Length)
                    Multiply(right, left, bits);
                else
                    Multiply(left, right, bits);
            }
        }
    }
}
