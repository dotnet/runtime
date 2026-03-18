// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        public static nuint Gcd(nuint left, nuint right)
        {
            // Executes the classic Euclidean algorithm.

            // https://en.wikipedia.org/wiki/Euclidean_algorithm

            while (right != 0)
            {
                nuint temp = left % right;
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
                return Gcd((uint)right, (uint)(left % right));

            return left;
        }

        public static nuint Gcd(ReadOnlySpan<nuint> left, nuint right)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right != 0);

            // A common divisor cannot be greater than right;
            // we compute the remainder and continue above...

            nuint temp = Remainder(left, right);

            return Gcd(right, temp);
        }

        public static void Gcd(ReadOnlySpan<nuint> left, ReadOnlySpan<nuint> right, Span<nuint> result)
        {
            Debug.Assert(left.Length >= 2);
            Debug.Assert(right.Length >= 2);
            Debug.Assert(CompareActual(left, right) >= 0);
            Debug.Assert(result.Length == left.Length);

            left.CopyTo(result);

            nuint[]? rightCopyFromPool = null;
            Span<nuint> rightCopy = (right.Length <= StackAllocThreshold ?
                                  stackalloc nuint[StackAllocThreshold]
                                  : rightCopyFromPool = ArrayPool<nuint>.Shared.Rent(right.Length)).Slice(0, right.Length);
            right.CopyTo(rightCopy);

            Gcd(result, rightCopy);

            if (rightCopyFromPool != null)
                ArrayPool<nuint>.Shared.Return(rightCopyFromPool);
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
                ulong x, y;

                ExtractDigits(left, right, out x, out y);

                uint a = 1U, b = 0U;
                uint c = 0U, d = 1U;

                int iteration = 0;

                // Lehmer's guessing
                while (y != 0)
                {
                    ulong q, r, s, t;

                    // Odd iteration
                    q = x / y;

                    if (q > 0xFFFFFFFF)
                        break;

                    r = a + q * c;
                    s = b + q * d;
                    t = x - q * y;

                    if (r > 0x7FFFFFFF || s > 0x7FFFFFFF)
                        break;
                    if (t < s || t + r > y - c)
                        break;

                    a = (uint)r;
                    b = (uint)s;
                    x = t;

                    ++iteration;
                    if (x == b)
                        break;

                    // Even iteration
                    q = y / x;

                    if (q > 0xFFFFFFFF)
                        break;

                    r = d + q * b;
                    s = c + q * a;
                    t = y - q * x;

                    if (r > 0x7FFFFFFF || s > 0x7FFFFFFF)
                        break;
                    if (t < s || t + r > x - b)
                        break;

                    d = (uint)r;
                    c = (uint)s;
                    y = t;

                    ++iteration;
                    if (y == c)
                        break;
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
                    var count = LehmerCore(left, right, a, b, c, d);
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
                    x = (ulong)right[0];
                    y = (ulong)left[0];

                    if (right.Length > 1)
                    {
                        x |= (ulong)right[1] << 32;
                        y |= (ulong)left[1] << 32;
                    }
                }
                else
                {
                    x = (ulong)right[0];
                    y = (ulong)left[0];
                }

                left = left.Slice(0, Overwrite(left, Gcd(x, y)));
                right.Clear();
            }

            left.CopyTo(result);
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

                nuint lo = unchecked((nuint)value);
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

                ulong xh = xBuffer[xBuffer.Length - 1];
                ulong xm = xBuffer[xBuffer.Length - 2];
                ulong xl = xBuffer[xBuffer.Length - 3];

                ulong yh, ym, yl;

                // arrange the bits
                switch (xBuffer.Length - yBuffer.Length)
                {
                    case 0:
                        yh = yBuffer[yBuffer.Length - 1];
                        ym = yBuffer[yBuffer.Length - 2];
                        yl = yBuffer[yBuffer.Length - 3];
                        break;

                    case 1:
                        yh = 0UL;
                        ym = yBuffer[yBuffer.Length - 1];
                        yl = yBuffer[yBuffer.Length - 2];
                        break;

                    case 2:
                        yh = 0UL;
                        ym = 0UL;
                        yl = yBuffer[yBuffer.Length - 1];
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

                ulong xh = (ulong)xBuffer[xBuffer.Length - 1];
                ulong xl = (ulong)xBuffer[xBuffer.Length - 2];

                ulong yh, yl;

                // arrange the bits
                switch (xBuffer.Length - yBuffer.Length)
                {
                    case 0:
                        yh = (ulong)yBuffer[yBuffer.Length - 1];
                        yl = (ulong)yBuffer[yBuffer.Length - 2];
                        break;

                    case 1:
                        yh = 0UL;
                        yl = (ulong)yBuffer[yBuffer.Length - 1];
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
                    x[i] = unchecked((nuint)xDigit);
                    y[i] = unchecked((nuint)yDigit);
                }
            }
            else
            {
                // Use Math.BigMul for widening multiplies instead of Int128
                // which compiles to much cheaper native mul instructions.
                // a,b,c,d are at most 31 bits, so each product fits in 95 bits.
                ulong ua = (ulong)a, ub = (ulong)b, uc = (ulong)c, ud = (ulong)d;
                long xCarry = 0, yCarry = 0;
                for (int i = 0; i < length; i++)
                {
                    ulong xi = (ulong)x[i];
                    ulong yi = (ulong)y[i];

                    // xDigit = a*xi - b*yi + xCarry (fits in ~97 signed bits)
                    ulong axi_hi = Math.BigMul(ua, xi, out ulong axi_lo);
                    ulong byi_hi = Math.BigMul(ub, yi, out ulong byi_lo);

                    ulong xlo = axi_lo - byi_lo;
                    long xhi = (long)(axi_hi - byi_hi) - (axi_lo < byi_lo ? 1L : 0L);

                    ulong xResultLo = xlo + unchecked((ulong)xCarry);
                    xhi += (xCarry >> 63) + (xResultLo < xlo ? 1L : 0L);

                    x[i] = unchecked((nuint)xResultLo);
                    xCarry = xhi;

                    // yDigit = d*yi - c*xi + yCarry
                    ulong dyi_hi = Math.BigMul(ud, yi, out ulong dyi_lo);
                    ulong cxi_hi = Math.BigMul(uc, xi, out ulong cxi_lo);

                    ulong ylo = dyi_lo - cxi_lo;
                    long yhi = (long)(dyi_hi - cxi_hi) - (dyi_lo < cxi_lo ? 1L : 0L);

                    ulong yResultLo = ylo + unchecked((ulong)yCarry);
                    yhi += (yCarry >> 63) + (yResultLo < ylo ? 1L : 0L);

                    y[i] = unchecked((nuint)yResultLo);
                    yCarry = yhi;
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
