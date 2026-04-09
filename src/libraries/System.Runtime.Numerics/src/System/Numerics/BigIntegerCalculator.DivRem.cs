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

        public static void Divide(ReadOnlySpan<nuint> left, nuint right, Span<nuint> quotient, out nuint remainder)
        {
            InitializeForDebug(quotient);
            nuint carry = 0;
            DivideCore(left, right, quotient, ref carry);
            remainder = carry;
        }

        public static void Divide(ReadOnlySpan<nuint> left, nuint right, Span<nuint> quotient)
        {
            InitializeForDebug(quotient);
            nuint carry = 0;
            DivideCore(left, right, quotient, ref carry);
        }

        private static void DivideCore(ReadOnlySpan<nuint> left, nuint right, Span<nuint> quotient, ref nuint carry)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(quotient.Length == left.Length);
            InitializeForDebug(quotient);

            // Executes the division for one big and one native-width integer.
            // Thus, we've similar code than below, but there is no loop for
            // processing the native-width integer, since it's a single element.

            for (int i = left.Length - 1; i >= 0; i--)
            {
                quotient[i] = DivRem(carry, left[i], right, out nuint rem);
                carry = rem;
            }
        }

        public static nuint Remainder(ReadOnlySpan<nuint> left, nuint right)
        {
            Debug.Assert(left.Length >= 1);

            // Same as above, but only computing the remainder.
            nuint carry = 0;
            for (int i = left.Length - 1; i >= 0; i--)
            {
                DivRem(carry, left[i], right, out carry);
            }

            return carry;
        }

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
                // Same as above, but only returning the quotient.

                // NOTE: left will get overwritten, we need a local copy
                // However, mutated left is not used afterwards, so use array pooling or stack alloc
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
                // Same as above, but only returning the remainder.

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

        private static void DivideGrammarSchool(Span<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> quotient)
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

            nuint divHi = right[^1];
            nuint divLo = right.Length > 1 ? right[^2] : 0;

            // We measure the leading zeros of the divisor
            int shift = (int)nuint.LeadingZeroCount(divHi);
            int backShift = BitsPerLimb - shift;

            // And, we make sure the most significant bit is set
            if (shift > 0)
            {
                nuint divNx = right.Length > 2 ? right[^3] : 0;

                divHi = (divHi << shift) | (divLo >> backShift);
                divLo = (divLo << shift) | (divNx >> backShift);
            }

            // Then, we divide all of the bits as we would do it using
            // pen and paper: guessing the next digit, subtracting, ...
            for (int i = left.Length; i >= right.Length; i--)
            {
                int n = i - right.Length;
                nuint t = (uint)i < (uint)left.Length ? left[i] : 0;

                nuint valHi1 = t;
                nuint valHi0 = left[i - 1];
                nuint valLo = i > 1 ? left[i - 2] : 0;

                // We shifted the divisor, we shift the dividend too
                if (shift > 0)
                {
                    nuint valNx = i > 2 ? left[i - 3] : 0;

                    valHi1 = (valHi1 << shift) | (valHi0 >> backShift);
                    valHi0 = (valHi0 << shift) | (valLo >> backShift);
                    valLo = (valLo << shift) | (valNx >> backShift);
                }

                // First guess for the current digit of the quotient,
                // which naturally must have only native-width bits...
                nuint digit = (valHi1 >= divHi) ? nuint.MaxValue : DivRem(valHi1, valHi0, divHi, out _);

                // Our first guess may be a little bit to big
                while (DivideGuessTooBig(digit, valHi1, valHi0, valLo, divHi, divLo))
                {
                    --digit;
                }

                if (digit > 0)
                {
                    // Now it's time to subtract our current quotient
                    nuint carry = SubtractDivisor(left.Slice(n), right, digit);
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
                {
                    quotient[n] = digit;
                }

                if ((uint)i < (uint)left.Length)
                {
                    left[i] = 0;
                }
            }
        }

        private static nuint AddDivisor(Span<nuint> left, ReadOnlySpan<nuint> right)
        {
            Debug.Assert(left.Length >= right.Length);

            // Repairs the dividend, if the last subtract was too much

            nuint carry = 0;

            for (int i = 0; i < right.Length; i++)
            {
                ref nuint leftElement = ref left[i];
                leftElement = AddWithCarry(leftElement, right[i], carry, out carry);
            }

            return carry;
        }

        private static nuint SubtractDivisor(Span<nuint> left, ReadOnlySpan<nuint> right, nuint q)
        {
            Debug.Assert(left.Length >= right.Length);

            return SubMul1(left, right, q);
        }

        private static bool DivideGuessTooBig(nuint q, nuint valHi1, nuint valHi0,
                                              nuint valLo, nuint divHi, nuint divLo)
        {
            // We multiply the two most significant limbs of the divisor
            // with the current guess for the quotient. If those are bigger
            // than the three most significant limbs of the current dividend
            // we return true, which means the current guess is still too big.

            nuint chkHiHi = nuint.BigMul(divHi, q, out nuint chkHiLo);
            nuint chkLoHi = nuint.BigMul(divLo, q, out nuint chkLoLo);

            chkHiLo += chkLoHi;
            if (chkHiLo < chkLoHi)
            {
                chkHiHi++;
            }

            return (chkHiHi > valHi1)
                || ((chkHiHi == valHi1) && ((chkHiLo > valHi0) || ((chkHiLo == valHi0) && (chkLoLo > valLo))));
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
