// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        public static uint Gcd(uint left, uint right)
        {
            // Executes the classic Euclidean algorithm.

            // https://en.wikipedia.org/wiki/Euclidean_algorithm

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
                return Gcd((uint)right, (uint)(left % right));

            return left;
        }

        public static uint Gcd(uint[] left, uint right)
        {
            Debug.Assert(left != null);
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right != 0);

            // A common divisor cannot be greater than right;
            // we compute the remainder and continue above...

            uint temp = Remainder(left, right);

            return Gcd(right, temp);
        }

        public static uint[] Gcd(uint[] left, uint[] right)
        {
            Debug.Assert(left != null);
            Debug.Assert(left.Length >= 2);
            Debug.Assert(right != null);
            Debug.Assert(right.Length >= 2);
            Debug.Assert(Compare(left, right) >= 0);

            BitsBuffer leftBuffer = new BitsBuffer(left.Length, left);
            BitsBuffer rightBuffer = new BitsBuffer(right.Length, right);

            Gcd(ref leftBuffer, ref rightBuffer);

            return leftBuffer.GetBits();
        }

        private static void Gcd(ref BitsBuffer left, ref BitsBuffer right)
        {
            Debug.Assert(left.GetLength() >= 2);
            Debug.Assert(right.GetLength() >= 2);
            Debug.Assert(left.GetLength() >= right.GetLength());

            // Executes Lehmer's gcd algorithm, but uses the most
            // significant bits to work with 64-bit (not 32-bit) values.
            // Furthermore we're using an optimized version due to Jebelean.

            // http://cacr.uwaterloo.ca/hac/about/chap14.pdf (see 14.4.2)
            // ftp://ftp.risc.uni-linz.ac.at/pub/techreports/1992/92-69.ps.gz

            while (right.GetLength() > 2)
            {
                ulong x, y;

                ExtractDigits(ref left, ref right, out x, out y);

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
                    left.Reduce(ref right);

                    BitsBuffer temp = left;
                    left = right;
                    right = temp;
                }
                else
                {
                    // Lehmer's step
                    LehmerCore(ref left, ref right, a, b, c, d);

                    if (iteration % 2 == 1)
                    {
                        // Ensure left is larger than right
                        BitsBuffer temp = left;
                        left = right;
                        right = temp;
                    }
                }
            }

            if (right.GetLength() > 0)
            {
                // Euclid's step
                left.Reduce(ref right);

                uint[] xBits = right.GetBits();
                uint[] yBits = left.GetBits();

                ulong x = ((ulong)xBits[1] << 32) | xBits[0];
                ulong y = ((ulong)yBits[1] << 32) | yBits[0];

                left.Overwrite(Gcd(x, y));
                right.Overwrite(0);
            }
        }

        private static void ExtractDigits(ref BitsBuffer xBuffer,
                                          ref BitsBuffer yBuffer,
                                          out ulong x, out ulong y)
        {
            Debug.Assert(xBuffer.GetLength() >= 3);
            Debug.Assert(yBuffer.GetLength() >= 3);
            Debug.Assert(xBuffer.GetLength() >= yBuffer.GetLength());

            // Extracts the most significant bits of x and y,
            // but ensures the quotient x / y does not change!

            uint[] xBits = xBuffer.GetBits();
            int xLength = xBuffer.GetLength();

            uint[] yBits = yBuffer.GetBits();
            int yLength = yBuffer.GetLength();

            ulong xh = xBits[xLength - 1];
            ulong xm = xBits[xLength - 2];
            ulong xl = xBits[xLength - 3];

            ulong yh, ym, yl;

            // arrange the bits
            switch (xLength - yLength)
            {
                case 0:
                    yh = yBits[yLength - 1];
                    ym = yBits[yLength - 2];
                    yl = yBits[yLength - 3];
                    break;

                case 1:
                    yh = 0UL;
                    ym = yBits[yLength - 1];
                    yl = yBits[yLength - 2];
                    break;

                case 2:
                    yh = 0UL;
                    ym = 0UL;
                    yl = yBits[yLength - 1];
                    break;

                default:
                    yh = 0UL;
                    ym = 0UL;
                    yl = 0UL;
                    break;
            }

            // Use all the bits but one, see [hac] 14.58 (ii)
            int z = LeadingZeros((uint)xh);

            x = ((xh << 32 + z) | (xm << z) | (xl >> 32 - z)) >> 1;
            y = ((yh << 32 + z) | (ym << z) | (yl >> 32 - z)) >> 1;

            Debug.Assert(x >= y);
        }

        private static void LehmerCore(ref BitsBuffer xBuffer,
                                       ref BitsBuffer yBuffer,
                                       long a, long b,
                                       long c, long d)
        {
            Debug.Assert(xBuffer.GetLength() >= 1);
            Debug.Assert(yBuffer.GetLength() >= 1);
            Debug.Assert(xBuffer.GetLength() >= yBuffer.GetLength());
            Debug.Assert(a <= 0x7FFFFFFF && b <= 0x7FFFFFFF);
            Debug.Assert(c <= 0x7FFFFFFF && d <= 0x7FFFFFFF);

            // Executes the combined calculation of Lehmer's step.

            uint[] x = xBuffer.GetBits();
            uint[] y = yBuffer.GetBits();

            int length = yBuffer.GetLength();

            long xCarry = 0L, yCarry = 0L;
            for (int i = 0; i < length; i++)
            {
                long xDigit = a * x[i] - b * y[i] + xCarry;
                long yDigit = d * y[i] - c * x[i] + yCarry;
                xCarry = xDigit >> 32;
                yCarry = yDigit >> 32;
                x[i] = unchecked((uint)xDigit);
                y[i] = unchecked((uint)yDigit);
            }

            xBuffer.Refresh(length);
            yBuffer.Refresh(length);
        }
    }
}
