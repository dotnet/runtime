// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        internal
#if DEBUG
        static // Mutable for unit testing...
#else
        const
#endif
        int DivideBurnikelZieglerThreshold = 64;

        public static void Divide(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> quotient, Span<nuint> remainder)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(quotient.Length == left.Length - right.Length + 1);
            Debug.Assert(remainder.Length == left.Length);
            InitializeForDebug(quotient);
            InitializeForDebug(remainder);

            if (right.Length < DivideBurnikelZieglerThreshold || left.Length - right.Length < DivideBurnikelZieglerThreshold)
            {
                left.CopyTo(remainder);
                DivideGrammarSchool(remainder, right, quotient);
            }
            else
            {
                DivideBurnikelZiegler(left, right, quotient, remainder);
            }
        }

        public static void Divide(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> quotient)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(quotient.Length == left.Length - right.Length + 1);
            InitializeForDebug(quotient);

            if (right.Length < DivideBurnikelZieglerThreshold || left.Length - right.Length < DivideBurnikelZieglerThreshold)
            {
                Span<nuint> leftCopy = BigInteger.RentedBuffer.Create(left.Length, out BigInteger.RentedBuffer leftCopyBuffer);
                left.CopyTo(leftCopy);

                DivideGrammarSchool(leftCopy, right, quotient);

                leftCopyBuffer.Dispose();
            }
            else
            {
                DivideBurnikelZiegler(left, right, quotient, default);
            }
        }

        public static void Remainder(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> remainder)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(remainder.Length == left.Length);
            InitializeForDebug(remainder);

            if (right.Length < DivideBurnikelZieglerThreshold || left.Length - right.Length < DivideBurnikelZieglerThreshold)
            {
                left.CopyTo(remainder);
                DivideGrammarSchool(remainder, right, default);
            }
            else
            {
                int quotientLength = left.Length - right.Length + 1;

                Span<nuint> quotient = BigInteger.RentedBuffer.Create(quotientLength, out BigInteger.RentedBuffer quotientBuffer);

                DivideBurnikelZiegler(left, right, quotient, remainder);

                quotientBuffer.Dispose();
            }
        }

        public static void DivideSpecial(
            ReadOnlySpan<nuint> left,
            ReadOnlySpan<nuint> right,
            Span<nuint> quotient,
            Span<nuint> remainder)
        {
            Debug.Assert(left.Length >= 16);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(quotient.Length == left.Length - right.Length + 1);
            Debug.Assert(remainder.Length == left.Length);
            Debug.Assert(right[0] == 0 || right[0] == nuint.MaxValue);
            Debug.Assert(!remainder.Overlaps(left));

            InitializeForDebug(quotient);
            InitializeForDebug(remainder);

            if (right[0] == nuint.MaxValue
                && TryDivideMersenne(left, right, quotient, remainder, remainderAliasesLeft: false))
            {
                return;
            }

            if (right.Length < DivideBurnikelZieglerThreshold
                || left.Length - right.Length < DivideBurnikelZieglerThreshold)
            {
                left.CopyTo(remainder);
                if (right[0] == 0)
                {
                    DivideGrammarSchoolSpecial(remainder, right, quotient);
                }
                else
                {
                    DivideGrammarSchool(remainder, right, quotient);
                }
                return;
            }

            Divide(left, right, quotient, remainder);
        }

        public static void DivideSpecial(
            ReadOnlySpan<nuint> left,
            ReadOnlySpan<nuint> right,
            Span<nuint> quotient)
        {
            Debug.Assert(left.Length >= 16);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(quotient.Length == left.Length - right.Length + 1);
            Debug.Assert(right[0] == 0 || right[0] == nuint.MaxValue);

            InitializeForDebug(quotient);

            if (right[0] == nuint.MaxValue
                && TryDivideMersenne(left, right, quotient, default, remainderAliasesLeft: false))
            {
                return;
            }

            if (right.Length < DivideBurnikelZieglerThreshold
                || left.Length - right.Length < DivideBurnikelZieglerThreshold)
            {
                Span<nuint> leftCopy = BigInteger.RentedBuffer.Create(
                    left.Length, out BigInteger.RentedBuffer leftCopyBuffer);
                left.CopyTo(leftCopy);

                if (right[0] == 0)
                {
                    DivideGrammarSchoolSpecial(leftCopy, right, quotient);
                }
                else
                {
                    DivideGrammarSchool(leftCopy, right, quotient);
                }
                leftCopyBuffer.Dispose();
                return;
            }

            Divide(left, right, quotient);
        }

        public static void RemainderSpecial(
            ReadOnlySpan<nuint> left,
            ReadOnlySpan<nuint> right,
            Span<nuint> remainder)
        {
            Debug.Assert(left.Length >= 16);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(remainder.Length == left.Length);
            Debug.Assert(right[0] == 0 || right[0] == nuint.MaxValue);
            Debug.Assert(!remainder.Overlaps(left));

            InitializeForDebug(remainder);

            if (right[0] == nuint.MaxValue
                && TryDivideMersenne(left, right, default, remainder, remainderAliasesLeft: false))
            {
                return;
            }

            if (right.Length < DivideBurnikelZieglerThreshold
                || left.Length - right.Length < DivideBurnikelZieglerThreshold)
            {
                left.CopyTo(remainder);

                if (right[0] == 0)
                {
                    DivideGrammarSchoolSpecial(remainder, right, default);
                }
                else
                {
                    DivideGrammarSchool(remainder, right, default);
                }
                return;
            }

            Remainder(left, right, remainder);
        }

        /// <summary>
        /// Logically equivalent to the following code.
        /// <code>
        /// quotient = left / right;
        /// left %= right;
        /// </code>
        /// </summary>
        private static void DivRem(Span<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> quotient)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(quotient.Length == left.Length - right.Length + 1
                || quotient.Length == 0);
            InitializeForDebug(quotient);

            if (left.Length >= 16
                && right[0] == nuint.MaxValue
                && TryDivideMersenne(left, right, quotient, left, remainderAliasesLeft: true))
            {
                return;
            }

            if (right.Length < DivideBurnikelZieglerThreshold || left.Length - right.Length < DivideBurnikelZieglerThreshold)
            {
                DivideGrammarSchool(left, right, quotient);
            }
            else
            {
                // NOTE: left will get overwritten, we need a local copy
                // However, mutated left is not used afterwards, so use array pooling or stack alloc
                Span<nuint> leftCopy = BigInteger.RentedBuffer.Create(left.Length, out BigInteger.RentedBuffer leftCopyBuffer);
                left.CopyTo(leftCopy);

                int quotientLength = quotient.Length > 0 ? 0 : left.Length - right.Length + 1;
                Span<nuint> quotientAllocated = BigInteger.RentedBuffer.Create(quotientLength, out BigInteger.RentedBuffer quotientActualBuffer);
                Span<nuint> quotientActual = quotient.Length > 0 ? quotient : quotientAllocated;

                DivideBurnikelZiegler(leftCopy, right, quotientActual, left);

                quotientActualBuffer.Dispose();
                leftCopyBuffer.Dispose();
            }
        }

        private static bool TryDivideMersenne(
            ReadOnlySpan<nuint> left,
            ReadOnlySpan<nuint> right,
            Span<nuint> quotient,
            Span<nuint> remainder,
            bool remainderAliasesLeft)
        {
            if (left.Length < 16
                || left.Length - right.Length < right.Length
                || right.Length < 2
                || right[0] != nuint.MaxValue
                || right[1] != nuint.MaxValue
                || right.ContainsAnyExcept(nuint.MaxValue))
            {
                return false;
            }

            DivideMersenne(left, right, quotient, remainder, remainderAliasesLeft);
            return true;
        }

        private static void DivideMersenne(
            ReadOnlySpan<nuint> left,
            ReadOnlySpan<nuint> right,
            Span<nuint> quotient,
            Span<nuint> remainder,
            bool remainderAliasesLeft)
        {
            // For X = B^k, X == 1 (mod X - 1), so the remainder is the sum of the
            // k-limb chunks reduced modulo X - 1. The quotient's chunks are the
            // corresponding rolling suffix sums plus floor(chunkSum / (X - 1)).
            int chunkLength = right.Length;
            int sumLength = chunkLength + 1;
            bool usesScratch = !remainderAliasesLeft && remainder.Length >= sumLength;
            int rentedLength = usesScratch ? 0 : sumLength;
            Span<nuint> rented = BigInteger.RentedBuffer.Create(rentedLength, out BigInteger.RentedBuffer sumBuffer);
            Span<nuint> sum = usesScratch
                ? remainder[..sumLength]
                : rented;

            sum.Clear();
            SumChunks(left, chunkLength, sum);

            nuint quotientAdjustment = GetQuotientAdjustment(sum);

            if (!quotient.IsEmpty)
            {
                quotient.Clear();

                nuint coefficientCarry = quotientAdjustment;
                int quotientOffset = 0;

                for (int chunkOffset = 0; chunkOffset + chunkLength < left.Length; chunkOffset += chunkLength)
                {
                    SubtractSelf(sum, left.Slice(chunkOffset, chunkLength));

                    nuint carry = coefficientCarry;

                    for (int i = 0; i < chunkLength; i++)
                    {
                        nuint digit = sum[i] + carry;
                        carry = digit < sum[i] ? 1 : (nuint)0;

                        if (quotientOffset + i < quotient.Length)
                        {
                            quotient[quotientOffset + i] = digit;
                        }
                        else
                        {
                            Debug.Assert(digit == 0);
                        }
                    }

                    coefficientCarry = sum[chunkLength] + carry;
                    quotientOffset += chunkLength;
                }

                if (quotientOffset < quotient.Length)
                {
                    quotient[quotientOffset] = coefficientCarry;
                }
                else
                {
                    Debug.Assert(coefficientCarry == 0);
                }
            }

            if (!remainder.IsEmpty)
            {
                if (!quotient.IsEmpty)
                {
                    sum.Clear();
                    SumChunks(left, chunkLength, sum);
                }

                ReduceSum(sum);

                if (!usesScratch)
                {
                    remainder.Clear();
                    sum[..chunkLength].CopyTo(remainder);
                }
                else
                {
                    remainder[chunkLength..].Clear();
                }
            }

            if (!usesScratch)
            {
                sumBuffer.Dispose();
            }

            static void SumChunks(ReadOnlySpan<nuint> value, int chunkLength, Span<nuint> sum)
            {
                for (int offset = 0; offset < value.Length; offset += chunkLength)
                {
                    AddSelf(sum, value.Slice(offset, Math.Min(chunkLength, value.Length - offset)));
                }
            }

            static nuint GetQuotientAdjustment(ReadOnlySpan<nuint> sum)
            {
                nuint adjustment = sum[^1];
                nuint carry = adjustment;
                bool allMaxValue = true;

                foreach (nuint limb in sum[..^1])
                {
                    nuint digit = limb + carry;
                    carry = digit < limb ? 1 : (nuint)0;
                    allMaxValue &= digit == nuint.MaxValue;
                }

                return adjustment + ((carry != 0 || allMaxValue) ? 1u : 0u);
            }

            static void ReduceSum(Span<nuint> sum)
            {
                Span<nuint> low = sum[..^1];
                nuint carry = sum[^1];
                bool allMaxValue = true;

                for (int i = 0; i < low.Length; i++)
                {
                    nuint digit = low[i] + carry;
                    carry = digit < low[i] ? 1 : (nuint)0;
                    low[i] = digit;
                    allMaxValue &= digit == nuint.MaxValue;
                }

                if (carry != 0)
                {
                    carry = 1;

                    int i = 0;
                    for (; carry != 0 && i < low.Length; i++)
                    {
                        nuint digit = low[i] + carry;
                        carry = digit < low[i] ? 1 : (nuint)0;
                        low[i] = digit;
                    }

                    Debug.Assert(carry == 0);
                }
                else if (allMaxValue)
                {
                    low.Clear();
                }
            }
        }

        private static void DivideBurnikelZiegler(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> quotient, Span<nuint> remainder)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(left.Length >= right.Length);
            Debug.Assert(quotient.Length == left.Length - right.Length + 1);
            Debug.Assert(remainder.Length == left.Length || remainder.Length == 0);

            // Executes the Burnikel-Ziegler algorithm for computing q = a / b.
            // Burnikel, C., Ziegler, J.: Fast recursive division. Research Report MPI-I-98-1-022, MPI Saarbrücken, 1998

            // Fast recursive division: Algorithm 3
            int n;
            {
                int m = (int)BitOperations.RoundUpToPowerOf2((uint)right.Length / (uint)DivideBurnikelZieglerThreshold + 1);

                int j = (right.Length + m - 1) / m; // Ceil(right.Length/m)
                n = j * m;
            }

            int sigmaDigit = n - right.Length;
            int sigmaSmall = (int)nuint.LeadingZeroCount(right[^1]);

            Span<nuint> b = BigInteger.RentedBuffer.Create(n, out BigInteger.RentedBuffer bBuffer);

            int aLength = left.Length + sigmaDigit;

            // if: LeadingZeroCount(left[^1]) < sigmaSmall, requires one more digit obviously.
            // if: LeadingZeroCount(left[^1]) == sigmaSmall, requires one more digit, because the leftmost bit of a must be 0.

            int leftLzc = (int)nuint.LeadingZeroCount(left[^1]);
            if (leftLzc <= sigmaSmall)
            {
                ++aLength;
            }

            Span<nuint> a = BigInteger.RentedBuffer.Create(aLength, out BigInteger.RentedBuffer aBuffer);

            static void Normalize(ReadOnlySpan<nuint> src, int sigmaDigit, int sigmaSmall, Span<nuint> bits)
            {
                Debug.Assert((uint)sigmaSmall <= BitsPerLimb);
                Debug.Assert(src.Length + sigmaDigit <= bits.Length);

                bits.Slice(0, sigmaDigit).Clear();
                Span<nuint> dst = bits.Slice(sigmaDigit);
                src.CopyTo(dst);
                dst.Slice(src.Length).Clear();

                if (sigmaSmall != 0)
                {
                    // Left shift
                    int carryShift = BitsPerLimb - sigmaSmall;
                    nuint carry = 0;

                    for (int i = 0; i < bits.Length; i++)
                    {
                        nuint carryTmp = bits[i] >> carryShift;
                        bits[i] = bits[i] << sigmaSmall | carry;
                        carry = carryTmp;
                    }

                    Debug.Assert(carry == 0);
                }
            }

            Normalize(left, sigmaDigit, sigmaSmall, a);
            Normalize(right, sigmaDigit, sigmaSmall, b);


            int t = Math.Max(2, (a.Length + n - 1) / n); // Max(2, Ceil(a.Length/n))
            Debug.Assert(t < a.Length || (t == a.Length && (nint)a[^1] >= 0));

            Span<nuint> r = BigInteger.RentedBuffer.Create(n + 1, out BigInteger.RentedBuffer rBuffer);

            Span<nuint> z = BigInteger.RentedBuffer.Create(2 * n, out BigInteger.RentedBuffer zBuffer);
            a.Slice((t - 2) * n).CopyTo(z);
            z.Slice(a.Length - (t - 2) * n).Clear();

            Span<nuint> quotientUpper = quotient.Slice((t - 2) * n);
            if (quotientUpper.Length < n)
            {
                Span<nuint> q = BigInteger.RentedBuffer.Create(n, out BigInteger.RentedBuffer qBuffer);

                BurnikelZieglerD2n1n(z, b, q, r);

                Debug.Assert(!q.Slice(quotientUpper.Length).ContainsAnyExcept(0u));
                q.Slice(0, quotientUpper.Length).CopyTo(quotientUpper);

                qBuffer.Dispose();
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

            zBuffer.Dispose();

            bBuffer.Dispose();

            aBuffer.Dispose();

            Debug.Assert(r[^1] == 0);
            Debug.Assert(!r.Slice(0, sigmaDigit).ContainsAnyExcept(0u));
            if (remainder.Length != 0)
            {
                Span<nuint> rt = r.Slice(sigmaDigit);
                remainder.Slice(rt.Length).Clear();

                if (sigmaSmall != 0)
                {
                    // Right shift
                    Debug.Assert((uint)sigmaSmall <= BitsPerLimb);

                    int carryShift = BitsPerLimb - sigmaSmall;
                    nuint carry = 0;

                    for (int i = rt.Length - 1; i >= 0; i--)
                    {
                        remainder[i] = rt[i] >> sigmaSmall | carry;
                        carry = rt[i] << carryShift;
                    }

                    Debug.Assert(carry == 0);
                }
                else
                {
                    rt.CopyTo(remainder);
                }
            }

            rBuffer.Dispose();
        }

        private static void BurnikelZieglerFallback(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> quotient, Span<nuint> remainder)
        {
            // Fast recursive division: Algorithm 1
            // 1. If n is odd or smaller than some convenient constant

            Debug.Assert(left.Length == 2 * right.Length);
            Debug.Assert(CompareActual(left.Slice(right.Length), right) < 0);
            Debug.Assert(quotient.Length == right.Length);
            Debug.Assert(remainder.Length >= right.Length + 1);
            Debug.Assert(right[^1] > 0);
            Debug.Assert(right.Length < DivideBurnikelZieglerThreshold);

            left = left.Slice(0, ActualLength(left));

            if (left.Length < right.Length)
            {
                quotient.Clear();
                left.CopyTo(remainder);
                remainder.Slice(left.Length).Clear();
            }
            else if (right.Length == 1)
            {
                nuint carry;

                if (quotient.Length < left.Length)
                {
                    Debug.Assert(quotient.Length + 1 == left.Length);
                    Debug.Assert(left[^1] < right[0]);

                    carry = left[^1];
                    DivideCore(left.Slice(0, quotient.Length), right[0], quotient, ref carry);
                }
                else
                {
                    carry = 0;
                    quotient.Slice(left.Length).Clear();
                    DivideCore(left, right[0], quotient, ref carry);
                }

                if (remainder.Length != 0)
                {
                    remainder.Slice(1).Clear();
                    remainder[0] = carry;
                }
            }
            else
            {
                Span<nuint> r1 = BigInteger.RentedBuffer.Create(left.Length, out BigInteger.RentedBuffer r1Buffer);

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

                r1Buffer.Dispose();
            }
        }

        private static void BurnikelZieglerD2n1n(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> quotient, Span<nuint> remainder)
        {
            // Fast recursive division: Algorithm 1
            Debug.Assert(left.Length == 2 * right.Length);
            Debug.Assert(CompareActual(left.Slice(right.Length), right) < 0);
            Debug.Assert(quotient.Length == right.Length);
            Debug.Assert(remainder.Length >= right.Length + 1);
            Debug.Assert(right[^1] > 0);

            if ((right.Length & 1) != 0 || right.Length < DivideBurnikelZieglerThreshold)
            {
                BurnikelZieglerFallback(left, right, quotient, remainder);
                return;
            }

            int halfN = right.Length >> 1;

            Span<nuint> r1 = BigInteger.RentedBuffer.Create(right.Length + 1, out BigInteger.RentedBuffer r1Buffer);

            BurnikelZieglerD3n2n(left.Slice(right.Length), left.Slice(halfN, halfN), right, quotient.Slice(halfN), r1);
            BurnikelZieglerD3n2n(r1.Slice(0, right.Length), left.Slice(0, halfN), right, quotient.Slice(0, halfN), remainder);

            r1Buffer.Dispose();
        }

        private static void BurnikelZieglerD3n2n(ReadOnlySpan<nuint> left12, ReadOnlySpan<nuint> left3, ReadOnlySpan<nuint> right, Span<nuint> quotient, Span<nuint> remainder)
        {
            // Fast recursive division: Algorithm 2
            Debug.Assert(right.Length % 2 == 0);
            Debug.Assert(left12.Length == right.Length);
            Debug.Assert(2 * left3.Length == right.Length);
            Debug.Assert(2 * quotient.Length == right.Length);
            Debug.Assert(remainder.Length >= right.Length + 1);
            Debug.Assert(right[^1] > 0);
            Debug.Assert(CompareActual(left12, right) < 0);

            int n = right.Length >> 1;

            ReadOnlySpan<nuint> a1 = left12.Slice(n);
            ReadOnlySpan<nuint> b1 = right.Slice(n);
            ReadOnlySpan<nuint> b2 = right.Slice(0, n);
            Span<nuint> r1 = remainder.Slice(n);
            Span<nuint> d = BigInteger.RentedBuffer.Create(right.Length, out BigInteger.RentedBuffer dBuffer);

            if (CompareActual(a1, b1) < 0)
            {
                BurnikelZieglerD2n1n(left12, b1, quotient, r1);

                d.Clear();
                MultiplyActual(quotient, b2, d);
            }
            else
            {
                Debug.Assert(CompareActual(a1, b1) == 0);
                quotient.Fill(nuint.MaxValue);

                ReadOnlySpan<nuint> a2 = left12.Slice(0, n);
                Add(a2, b1, r1);

                d.Slice(0, n).Clear();
                b2.CopyTo(d.Slice(n));
                SubtractSelf(d, b2);
            }

            // R = [R1, A3]
            left3.CopyTo(remainder.Slice(0, n));

            Span<nuint> rr = remainder.Slice(0, d.Length + 1);

            while (CompareActual(rr, d) < 0)
            {
                AddSelf(rr, right);
                int qi = -1;
                while (quotient[++qi] == 0) ;
                Debug.Assert((uint)qi < (uint)quotient.Length);
                --quotient[qi];
                quotient.Slice(0, qi).Fill(nuint.MaxValue);
            }

            SubtractSelf(rr, d);

            dBuffer.Dispose();

            static void MultiplyActual(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> bits)
            {
                Debug.Assert(bits.Length == left.Length + right.Length);

                left = left.Slice(0, ActualLength(left));
                right = right.Slice(0, ActualLength(right));
                bits = bits.Slice(0, left.Length + right.Length);

                Multiply(left, right, bits);
            }
        }
    }
}
