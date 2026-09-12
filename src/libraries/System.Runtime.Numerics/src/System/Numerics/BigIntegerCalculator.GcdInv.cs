// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        public static nuint Gcd(nuint left, nuint right)
        {
            // Executes the classic Euclidean algorithm.
            // https://en.wikipedia.org/wiki/Euclidean_algorithm

            if (nint.Size == 8)
            {
                // Use 64-bit division until right fits in 32-bit, then
                // switch to cheaper 32-bit division for the remainder.
                while (right > uint.MaxValue)
                {
                    nuint temp = left % right;
                    left = right;
                    right = temp;
                }

                if (right != 0)
                {
                    return Gcd((uint)right, (uint)(left % right));
                }

                return left;
            }

            while (right != 0)
            {
                nuint temp = left % right;
                left = right;
                right = temp;
            }

            return left;
        }

        private static uint Gcd(uint left, uint right)
        {
            while (right != 0)
            {
                uint temp = left % right;
                left = right;
                right = temp;
            }

            return left;
        }

        public static ulong Gcd(ulong left, ulong right)
        {
            // Same as above, but for 64-bit values.

            while (right > 0xFFFFFFFF)
            {
                ulong temp = left % right;
                left = right;
                right = temp;
            }

            if (right != 0)
            {
                return Gcd((uint)right, (uint)(left % right));
            }

            return left;
        }

        public static nuint Gcd(ReadOnlySpan<nuint> left, nuint right)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right != 0);

            // A common divisor cannot be greater than right;
            // we compute the remainder and continue above...

            nuint remainder = Remainder(left, right);
            return Gcd(right, remainder);
        }

        public static void Gcd(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> result)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right.Length >= 1);
            Debug.Assert(CompareActual(left, right) >= 0);
            Debug.Assert(result.Length == left.Length);

            int commonOffset = GetCommonLimbOffset(left, right);
            if (commonOffset != 0)
            {
                result[..commonOffset].Clear();
                Gcd(left[commonOffset..], right[commonOffset..], result[commonOffset..]);
                return;
            }

            if (right.Length == 1)
            {
                result.Clear();
                result[0] = Gcd(left, right[0]);
                return;
            }

            left.CopyTo(result);

            Span<nuint> rightCopy = BigInteger.RentedBuffer.Create(right.Length, out BigInteger.RentedBuffer rightCopyBuffer);
            right.CopyTo(rightCopy);

            Gcd(result, rightCopy);

            rightCopyBuffer.Dispose();
        }

        private static void Gcd(Span<nuint> left, Span<nuint> right)
        {
            Debug.Assert(left.Length >= 2);
            Debug.Assert(right.Length >= 2);
            Debug.Assert(left.Length >= right.Length);

            Span<nuint> result = left;   //keep result buffer untouched during computation

            // Executes Lehmer's gcd algorithm, but uses the most
            // significant bits to work with 64-bit (not 32-bit) values.
            // Furthermore we're using an optimized version due to Jebelean.

            // http://cacr.uwaterloo.ca/hac/about/chap14.pdf (see 14.4.2)
            // ftp://ftp.risc.uni-linz.ac.at/pub/techreports/1992/92-69.ps.gz

            while (right.Length > (nint.Size == 4 ? 2 : 1))
            {

                ExtractDigits(left, right, out ulong x, out ulong y);

                uint a = 1U, b = 0U;
                uint c = 0U, d = 1U;

                int iteration = 0;

                // Lehmer's guessing: use top digits to compute a 2x2 matrix (a,b,c,d)
                // that approximates several GCD steps. Stop when the quotient or
                // matrix entries would overflow, or when the Jebelean termination
                // condition (t < s || t + r > y - c) indicates the guess may be wrong.
                while (y != 0)
                {
                    ulong q, r, s, t;

                    // Odd iteration
                    q = x / y;

                    if (q > 0xFFFFFFFF)
                    {
                        break;
                    }

                    r = a + q * c;
                    s = b + q * d;
                    t = x - q * y;

                    if (r > 0x7FFFFFFF || s > 0x7FFFFFFF || t < s || t + r > y - c)
                    {
                        break;
                    }

                    a = (uint)r;
                    b = (uint)s;
                    x = t;

                    ++iteration;
                    if (x == b)
                    {
                        break;
                    }

                    // Even iteration
                    q = y / x;

                    if (q > 0xFFFFFFFF)
                    {
                        break;
                    }

                    r = d + q * b;
                    s = c + q * a;
                    t = y - q * x;

                    if (r > 0x7FFFFFFF || s > 0x7FFFFFFF || t < s || t + r > x - b)
                    {
                        break;
                    }

                    d = (uint)r;
                    c = (uint)s;
                    y = t;

                    ++iteration;
                    if (y == c)
                    {
                        break;
                    }
                }

                if (b == 0)
                {
                    // Euclid's step
                    left = left.Slice(0, Reduce(left, right));

                    Span<nuint> temp = left;
                    left = right;
                    right = temp;
                }
                else
                {
                    // Lehmer's step
                    int count = LehmerCore(left, right, a, b, c, d);
                    left = left.Slice(0, Refresh(left, count));
                    right = right.Slice(0, Refresh(right, count));

                    if (iteration % 2 == 1)
                    {
                        // Ensure left is larger than right
                        Span<nuint> temp = left;
                        left = right;
                        right = temp;
                    }
                }
            }

            if (right.Length > 0)
            {
                // Euclid's step
                Reduce(left, right);

                ulong x, y;

                if (nint.Size == 4)
                {
                    x = right[0];
                    y = left[0];

                    if (right.Length > 1)
                    {
                        x |= (ulong)right[1] << 32;
                        y |= (ulong)left[1] << 32;
                    }
                }
                else
                {
                    x = right[0];
                    y = left[0];
                }

                left = left.Slice(0, Overwrite(left, Gcd(x, y)));
                right.Clear();
            }

            left.CopyTo(result);
        }

        public static bool ModInverse(ReadOnlySpan<nuint> value, ReadOnlySpan<nuint> modulus, Span<nuint> result)
        {
            Debug.Assert(value.Length >= 1);
            Debug.Assert(modulus.Length >= 1);
            Debug.Assert(ActualLength(value) >= 1);
            Debug.Assert(CompareActual(value, modulus) < 0);
            Debug.Assert(result.Length == modulus.Length);

            // Executes the extended Euclidean algorithm, reusing the Lehmer guessing step
            // and Jebelean termination condition from Gcd to advance several Euclid steps
            // at a time.
            //
            // Alongside the remainder sequence r[0] = modulus, r[1] = value this carries a
            // sequence of cofactor magnitudes
            //
            //     t[0] = 0, t[1] = 1, t[i+1] = t[i-1] + q[i] * t[i]
            //
            // which satisfies the invariant
            //
            //     r[i] == (-1)^(i+1) * t[i] * value   (mod modulus)
            //
            // Every t[i] is non-negative and the recurrence only ever adds, so cofactors
            // stay unsigned like the rest of the calculator and the alternating sign is
            // carried as the parity of i alone. The companion identity
            //
            //     t[i] * r[i-1] + t[i-1] * r[i] == modulus
            //
            // bounds every cofactor by the modulus, so one limb beyond the modulus is
            // always enough to hold one, including while it is being accumulated.

            int limbCount = modulus.Length;
            int cofactorLength = limbCount + 1;

            Span<nuint> x = BigInteger.RentedBuffer.Create(limbCount, out BigInteger.RentedBuffer xBuffer);
            Span<nuint> y = BigInteger.RentedBuffer.Create(limbCount, out BigInteger.RentedBuffer yBuffer);
            Span<nuint> u = BigInteger.RentedBuffer.Create(cofactorLength, out BigInteger.RentedBuffer uBuffer);
            Span<nuint> v = BigInteger.RentedBuffer.Create(cofactorLength, out BigInteger.RentedBuffer vBuffer);
            Span<nuint> cofactorLow = BigInteger.RentedBuffer.Create(cofactorLength, out BigInteger.RentedBuffer cofactorLowBuffer);
            Span<nuint> cofactorHigh = BigInteger.RentedBuffer.Create(cofactorLength, out BigInteger.RentedBuffer cofactorHighBuffer);
            Span<nuint> quotient = BigInteger.RentedBuffer.Create(limbCount, out BigInteger.RentedBuffer quotientBuffer);
            Span<nuint> remainder = BigInteger.RentedBuffer.Create(limbCount, out BigInteger.RentedBuffer remainderBuffer);
            Span<nuint> product = BigInteger.RentedBuffer.Create(limbCount + cofactorLength, out BigInteger.RentedBuffer productBuffer);

            modulus.CopyTo(x);
            value.CopyTo(y.Slice(0, value.Length));

            int xLength = ActualLength(x);
            int yLength = ActualLength(y);

            v[0] = 1;

            // Parity of i, the index of x within the remainder sequence. x starts as
            // r[0] = modulus, so the index starts even.
            bool oddIndex = false;

            // Lehmer's guess needs the leading limbs that ExtractDigits reads.
            int lehmerMinimum = nint.Size == 4 ? 3 : 2;

            while (yLength > 0)
            {
                if (xLength >= lehmerMinimum && yLength >= lehmerMinimum)
                {
                    ExtractDigits(x.Slice(0, xLength), y.Slice(0, yLength), out ulong xDigits, out ulong yDigits);

                    uint a = 1U, b = 0U;
                    uint c = 0U, d = 1U;

                    int iteration = 0;

                    // Lehmer's guessing: use top digits to compute a 2x2 matrix (a,b,c,d)
                    // that approximates several GCD steps. Stop when the quotient or
                    // matrix entries would overflow, or when the Jebelean termination
                    // condition (t < s || t + r > y - c) indicates the guess may be wrong.
                    while (yDigits != 0)
                    {
                        ulong q, r, s, t;

                        // Odd iteration
                        q = xDigits / yDigits;

                        if (q > 0xFFFFFFFF)
                        {
                            break;
                        }

                        r = a + q * c;
                        s = b + q * d;
                        t = xDigits - q * yDigits;

                        if (r > 0x7FFFFFFF || s > 0x7FFFFFFF || t < s || t + r > yDigits - c)
                        {
                            break;
                        }

                        a = (uint)r;
                        b = (uint)s;
                        xDigits = t;

                        ++iteration;
                        if (xDigits == b)
                        {
                            break;
                        }

                        // Even iteration
                        q = yDigits / xDigits;

                        if (q > 0xFFFFFFFF)
                        {
                            break;
                        }

                        r = d + q * b;
                        s = c + q * a;
                        t = yDigits - q * xDigits;

                        if (r > 0x7FFFFFFF || s > 0x7FFFFFFF || t < s || t + r > xDigits - b)
                        {
                            break;
                        }

                        d = (uint)r;
                        c = (uint)s;
                        yDigits = t;

                        ++iteration;
                        if (yDigits == c)
                        {
                            break;
                        }
                    }

                    if (b != 0)
                    {
                        // Lehmer's step. The remainders take the signed matrix, so the
                        // cofactor magnitudes take the same matrix with the signs dropped:
                        // the alternating signs of the two cofactors cancel the two
                        // subtractions exactly.
                        int count = LehmerCore(x.Slice(0, xLength), y.Slice(0, yLength), a, b, c, d);
                        xLength = Refresh(x.Slice(0, xLength), count);
                        yLength = Refresh(y.Slice(0, yLength), count);

                        CombineCofactors(u, v, a, b, cofactorLow);
                        CombineCofactors(u, v, c, d, cofactorHigh);
                        cofactorLow.CopyTo(u);
                        cofactorHigh.CopyTo(v);

                        if ((iteration & 1) != 0)
                        {
                            // The transformation advanced the index by an odd number of
                            // steps, so x now holds the later remainder. Swapping restores
                            // the ordering and flips the parity in lockstep.
                            Span<nuint> swapRemainder = x;
                            x = y;
                            y = swapRemainder;

                            Span<nuint> swapCofactor = u;
                            u = v;
                            v = swapCofactor;

                            (xLength, yLength) = (yLength, xLength);
                            oddIndex = !oddIndex;
                        }

                        continue;
                    }
                }

                // Euclid's step, also used whenever the operands are too short for
                // Lehmer's guess to read the leading limbs it needs.
                Span<nuint> stepQuotient = quotient.Slice(0, xLength - yLength + 1);
                Span<nuint> stepRemainder = remainder.Slice(0, xLength);

                Divide(x.Slice(0, xLength), y.Slice(0, yLength), stepQuotient, stepRemainder);

                int quotientLength = ActualLength(stepQuotient);
                Debug.Assert(quotientLength >= 1, "x >= y, so the quotient is at least one.");

                // v' = u + q * v, computed before the buffers are rotated.
                if (quotientLength == 1)
                {
                    AddScaled(u, v, stepQuotient[0], cofactorLow);
                }
                else
                {
                    product.Clear();
                    Multiply(stepQuotient.Slice(0, quotientLength), v, product);
                    AddSelf(product, u);

                    Debug.Assert(!product.Slice(cofactorLength).ContainsAnyExcept((nuint)0));
                    product.Slice(0, cofactorLength).CopyTo(cofactorLow);
                }

                // (x, y) <- (y, x mod y) and (u, v) <- (v, u + q * v)
                stepRemainder.Slice(0, yLength).CopyTo(x);
                x.Slice(yLength).Clear();
                xLength = yLength;

                Span<nuint> stepTemp = x;
                x = y;
                y = stepTemp;

                (xLength, yLength) = (yLength, xLength);
                xLength = ActualLength(x.Slice(0, xLength));
                yLength = ActualLength(y.Slice(0, yLength));

                v.CopyTo(u);
                cofactorLow.CopyTo(v);

                oddIndex = !oddIndex;
            }

            // The last non-zero remainder is the greatest common divisor.
            bool coprime = xLength == 1 && x[0] == 1;

            if (coprime)
            {
                // r[k] == 1 == (-1)^(k+1) * t[k] * value, so the inverse is t[k] when k is
                // odd and modulus - t[k] when k is even.
                int cofactorActualLength = ActualLength(u);
                Debug.Assert(cofactorActualLength >= 1 && cofactorActualLength <= limbCount);

                if (oddIndex)
                {
                    u.Slice(0, cofactorActualLength).CopyTo(result);
                    result.Slice(cofactorActualLength).Clear();
                }
                else
                {
                    modulus.CopyTo(result);
                    SubtractSelf(result, u.Slice(0, cofactorActualLength));
                }
            }

            productBuffer.Dispose();
            remainderBuffer.Dispose();
            quotientBuffer.Dispose();
            cofactorHighBuffer.Dispose();
            cofactorLowBuffer.Dispose();
            vBuffer.Dispose();
            uBuffer.Dispose();
            yBuffer.Dispose();
            xBuffer.Dispose();

            return coprime;
        }

        public static void ModInversePowerOfTwo(ReadOnlySpan<nuint> value, int exponent, Span<nuint> result)
        {
            Debug.Assert(value.Length >= 1);
            Debug.Assert((value[0] & 1) != 0);
            Debug.Assert(exponent >= 1);
            Debug.Assert(result.Length == (exponent + BitsPerLimb - 1) / BitsPerLimb);
            Debug.Assert(ActualLength(value) <= result.Length);

            // Executes Hensel lifting, in the Newton iteration form, on the 2-adic inverse.
            //
            // If value * x == 1 (mod 2^m), then x' = x * (2 - value * x) satisfies
            // value * x' == 1 (mod 2^2m): writing value * x = 1 + e * 2^m gives
            //
            //     value * x' = (1 + e * 2^m) * (1 - e * 2^m) = 1 - e^2 * 2^2m
            //
            // so each step doubles the number of correct bits. Only odd values are
            // invertible modulo a power of two, which the caller has already established.

            int limbCount = result.Length;

            result.Clear();

            // Seed with the inverse modulo a single limb. ComputeMontgomeryInverse runs the
            // same iteration to full limb width, but returns the negated inverse because
            // that is what Montgomery reduction consumes; negating it back recovers the
            // plain 2-adic inverse of the low limb.
            result[0] = (nuint)0 - ComputeMontgomeryInverse(value[0]);

            if (limbCount > 1)
            {
                Span<nuint> factor = BigInteger.RentedBuffer.Create(limbCount, out BigInteger.RentedBuffer factorBuffer);
                Span<nuint> product = BigInteger.RentedBuffer.Create(limbCount * 2, out BigInteger.RentedBuffer productBuffer);

                int valueLength = ActualLength(value);

                // The seed is correct to one whole limb, and the iteration doubles that
                // until it covers the modulus, so this terminates after exactly
                // ceil(log2(limbCount)) steps with correctLimbs == limbCount.
                int correctLimbs = 1;

                while (correctLimbs < limbCount)
                {
                    int nextLimbs = Math.Min(correctLimbs * 2, limbCount);
                    int resultLength = ActualLength(result.Slice(0, correctLimbs));
                    int valueFactorLength = Math.Min(valueLength, nextLimbs);

                    // factor = 2 - value * result, modulo the limb radix to nextLimbs.
                    // Dropping limbs above nextLimbs from either operand is exactly the
                    // truncation the modular reduction asks for. Multiply wants a
                    // destination of exactly the product width, so each call gets its own
                    // slice; a product narrower than nextLimbs simply leaves the cleared
                    // high limbs of the destination alone.
                    int lowWidth = valueFactorLength + resultLength;
                    Span<nuint> lowProduct = product.Slice(0, lowWidth);
                    lowProduct.Clear();

                    Multiply(value.Slice(0, valueFactorLength), result.Slice(0, resultLength), lowProduct);

                    factor.Slice(0, nextLimbs).Clear();
                    lowProduct.Slice(0, Math.Min(lowWidth, nextLimbs)).CopyTo(factor);
                    SubtractFromTwo(factor.Slice(0, nextLimbs));

                    // result = result * factor, again modulo the limb radix to nextLimbs.
                    int highWidth = nextLimbs + resultLength;
                    Span<nuint> highProduct = product.Slice(0, highWidth);
                    highProduct.Clear();

                    Multiply(factor.Slice(0, nextLimbs), result.Slice(0, resultLength), highProduct);

                    Debug.Assert(highWidth >= nextLimbs);
                    highProduct.Slice(0, nextLimbs).CopyTo(result);

                    correctLimbs = nextLimbs;
                }

                Debug.Assert(correctLimbs == limbCount);

                productBuffer.Dispose();
                factorBuffer.Dispose();
            }

            // The lift works to whole limbs, so trim the top limb back to the bits the
            // modulus actually spans. Reducing the inverse modulo 2^exponent leaves the
            // congruence intact because 2^exponent divides the limb radix to limbCount.
            int topBits = exponent - ((limbCount - 1) * BitsPerLimb);
            Debug.Assert(topBits >= 1 && topBits <= BitsPerLimb);

            if (topBits < BitsPerLimb)
            {
                result[limbCount - 1] &= ((nuint)1 << topBits) - 1;
            }

            // The inverse of an odd value is odd, so it is never zero.
            Debug.Assert((result[0] & 1) != 0);
        }

        private static void SubtractFromTwo(Span<nuint> bits)
        {
            // Computes 2 - bits modulo the limb radix to bits.Length. The two's complement
            // of t is ~t + 1, so 2 - t is ~t + 3, with the carry out of the top limb
            // dropped because that is the modular reduction.

            nuint carry = 3;

            for (int i = 0; i < bits.Length; i++)
            {
                nuint complement = ~bits[i];
                nuint digit = complement + carry;

                // Unsigned addition wrapped exactly when the sum fell below an addend.
                carry = digit < complement ? 1U : 0U;
                bits[i] = digit;
            }
        }

        private static void CombineCofactors(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right,
                                             nuint leftMultiplier, nuint rightMultiplier,
                                             Span<nuint> destination)
        {
            Debug.Assert(left.Length == right.Length);
            Debug.Assert(destination.Length == left.Length);
            Debug.Assert(leftMultiplier <= 0x7FFFFFFF);
            Debug.Assert(rightMultiplier <= 0x7FFFFFFF);

            // Computes leftMultiplier * left + rightMultiplier * right. Both multipliers
            // are at most 31 bits, but a limb is 32 bits wide on a 32-bit process, so the
            // accumulator has to be wider than a limb on either architecture. With both
            // multipliers below 2^31 the running carry stays below 2^32, so no term ever
            // approaches the top of the accumulator.

            UInt128 carry = 0;

            for (int i = 0; i < left.Length; i++)
            {
                UInt128 digit = ((UInt128)leftMultiplier * left[i]) + ((UInt128)rightMultiplier * right[i]) + carry;
                destination[i] = (nuint)digit;
                carry = digit >> BitsPerLimb;
            }

            // The result is another cofactor, so it is bounded by the modulus and cannot
            // overflow the spare limb the destination carries.
            Debug.Assert(carry == 0);
        }

        private static void AddScaled(ReadOnlySpan<nuint> addend, ReadOnlySpan<nuint> left,
                                      nuint multiplier, Span<nuint> destination)
        {
            Debug.Assert(addend.Length == left.Length);
            Debug.Assert(destination.Length == left.Length);

            // Computes addend + multiplier * left for a full-width multiplier. Writing L
            // for the limb radix, every accumulator term is bounded by
            // (L-1) + (L-1)^2 + (L-1) == L^2 - 1, so a carry stays below L and the
            // accumulator stays within twice the width of a limb. A second full-width
            // multiplier would not fit, which is why the two-multiplier form above is
            // restricted to the 31-bit entries Lehmer's guess produces.

            UInt128 carry = 0;

            for (int i = 0; i < left.Length; i++)
            {
                UInt128 digit = addend[i] + ((UInt128)multiplier * left[i]) + carry;
                destination[i] = (nuint)digit;
                carry = digit >> BitsPerLimb;
            }

            Debug.Assert(carry == 0);
        }

        private static int Overwrite(Span<nuint> buffer, ulong value)
        {
            if (nint.Size == 4)
            {
                Debug.Assert(buffer.Length >= 2);

                if (buffer.Length > 2)
                {
                    // Ensure leading zeros in little-endian
                    buffer.Slice(2).Clear();
                }

                nuint lo = (nuint)value;
                nuint hi = (nuint)(value >> 32);

                buffer[1] = hi;
                buffer[0] = lo;
                return hi != 0 ? 2 : lo != 0 ? 1 : 0;
            }
            else
            {
                Debug.Assert(buffer.Length >= 1);

                if (buffer.Length > 1)
                {
                    // Ensure leading zeros in little-endian
                    buffer.Slice(1).Clear();
                }

                buffer[0] = (nuint)value;
                return value != 0 ? 1 : 0;
            }
        }

        private static void ExtractDigits(ReadOnlySpan<nuint> xBuffer,
                                          ReadOnlySpan<nuint> yBuffer,
                                          out ulong x, out ulong y)
        {
            // Extracts the most significant bits of x and y,
            // but ensures the quotient x / y does not change!

            if (nint.Size == 4)
            {
                Debug.Assert(xBuffer.Length >= 3);
                Debug.Assert(yBuffer.Length >= 3);
                Debug.Assert(xBuffer.Length >= yBuffer.Length);

                ulong xh = xBuffer[^1];
                ulong xm = xBuffer[^2];
                ulong xl = xBuffer[^3];

                ulong yh, ym, yl;

                // arrange the bits
                switch (xBuffer.Length - yBuffer.Length)
                {
                    case 0:
                        yh = yBuffer[^1];
                        ym = yBuffer[^2];
                        yl = yBuffer[^3];
                        break;

                    case 1:
                        yh = 0UL;
                        ym = yBuffer[^1];
                        yl = yBuffer[^2];
                        break;

                    case 2:
                        yh = 0UL;
                        ym = 0UL;
                        yl = yBuffer[^1];
                        break;

                    default:
                        yh = 0UL;
                        ym = 0UL;
                        yl = 0UL;
                        break;
                }

                // Use all the bits but one, see [hac] 14.58 (ii)
                int z = BitOperations.LeadingZeroCount((uint)xh);

                x = ((xh << 32 + z) | (xm << z) | (xl >> 32 - z)) >> 1;
                y = ((yh << 32 + z) | (ym << z) | (yl >> 32 - z)) >> 1;
            }
            else
            {
                Debug.Assert(xBuffer.Length >= 2);
                Debug.Assert(yBuffer.Length >= 2);
                Debug.Assert(xBuffer.Length >= yBuffer.Length);

                ulong xh = xBuffer[^1];
                ulong xl = xBuffer[^2];

                ulong yh, yl;

                // arrange the bits
                switch (xBuffer.Length - yBuffer.Length)
                {
                    case 0:
                        yh = yBuffer[^1];
                        yl = yBuffer[^2];
                        break;

                    case 1:
                        yh = 0UL;
                        yl = yBuffer[^1];
                        break;

                    default:
                        yh = 0UL;
                        yl = 0UL;
                        break;
                }

                // Use all the bits but one, see [hac] 14.58 (ii)
                int z = BitOperations.LeadingZeroCount(xh);

                if (z == 0)
                {
                    x = xh >> 1;
                    y = yh >> 1;
                }
                else
                {
                    x = ((xh << z) | (xl >> (64 - z))) >> 1;
                    y = ((yh << z) | (yl >> (64 - z))) >> 1;
                }
            }

            Debug.Assert(x >= y);
        }

        private static int LehmerCore(Span<nuint> x,
                                      Span<nuint> y,
                                      long a, long b,
                                      long c, long d)
        {
            Debug.Assert(x.Length >= 1);
            Debug.Assert(y.Length >= 1);
            Debug.Assert(x.Length >= y.Length);
            Debug.Assert(a <= 0x7FFFFFFF && b <= 0x7FFFFFFF);
            Debug.Assert(c <= 0x7FFFFFFF && d <= 0x7FFFFFFF);

            // Executes the combined calculation of Lehmer's step.

            int length = y.Length;

            if (nint.Size == 4)
            {
                long xCarry = 0L, yCarry = 0L;
                for (int i = 0; i < length; i++)
                {
                    long xDigit = a * (long)x[i] - b * (long)y[i] + xCarry;
                    long yDigit = d * (long)y[i] - c * (long)x[i] + yCarry;
                    xCarry = xDigit >> 32;
                    yCarry = yDigit >> 32;
                    x[i] = (nuint)xDigit;
                    y[i] = (nuint)yDigit;
                }
            }
            else if (BitConverter.IsLittleEndian)
            {
                // On 64-bit little-endian, reinterpret the nuint limbs as uint halves.
                // Since a,b,c,d are at most 31 bits and each half is 32 bits,
                // each product fits in 63 bits and the full expression fits in long.
                // This matches the 32-bit path's arithmetic but operates on the
                // raw memory of 64-bit limbs (little-endian stores low half first).
                Span<uint> x32 = MemoryMarshal.Cast<nuint, uint>(x);
                Span<uint> y32 = MemoryMarshal.Cast<nuint, uint>(y);
                int length32 = length * 2;

                long xCarry = 0L, yCarry = 0L;
                for (int i = 0; i < length32; i++)
                {
                    long xDigit = a * x32[i] - b * y32[i] + xCarry;
                    long yDigit = d * y32[i] - c * x32[i] + yCarry;
                    xCarry = xDigit >> 32;
                    yCarry = yDigit >> 32;
                    x32[i] = (uint)xDigit;
                    y32[i] = (uint)yDigit;
                }
            }
            else
            {
                // Big-endian fallback: use Int128 for widening arithmetic.
                Int128 xCarry = 0, yCarry = 0;
                for (int i = 0; i < length; i++)
                {
                    Int128 xDigit = a * (Int128)x[i] - b * (Int128)y[i] + xCarry;
                    Int128 yDigit = d * (Int128)y[i] - c * (Int128)x[i] + yCarry;
                    xCarry = xDigit >> 64;
                    yCarry = yDigit >> 64;
                    x[i] = (nuint)(ulong)xDigit;
                    y[i] = (nuint)(ulong)yDigit;
                }
            }

            return length;
        }

        private static int Refresh(Span<nuint> bits, int maxLength)
        {
            Debug.Assert(bits.Length >= maxLength);

            if (bits.Length > maxLength)
            {
                // Ensure leading zeros
                bits.Slice(maxLength).Clear();
            }

            return ActualLength(bits.Slice(0, maxLength));
        }
    }
}
