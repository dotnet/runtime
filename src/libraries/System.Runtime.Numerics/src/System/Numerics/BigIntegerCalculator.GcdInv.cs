// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
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

        public static uint Gcd(ReadOnlySpan<uint> left, uint right)
        {
            Debug.Assert(left.Length >= 1);
            Debug.Assert(right != 0);

            // A common divisor cannot be greater than right;
            // we compute the remainder and continue above...

            uint temp = Remainder(left, right);

            return Gcd(right, temp);
        }

        public static void Gcd(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> result)
        {
            Debug.Assert(left.Length >= 2);
            Debug.Assert(right.Length >= 2);
            Debug.Assert(Compare(left, right) >= 0);
            Debug.Assert(result.Length == left.Length);

            left.CopyTo(result);

            uint[]? rightCopyFromPool = null;
            Span<uint> rightCopy = (right.Length <= StackAllocThreshold ?
                                  stackalloc uint[StackAllocThreshold]
                                  : rightCopyFromPool = ArrayPool<uint>.Shared.Rent(right.Length)).Slice(0, right.Length);
            right.CopyTo(rightCopy);

            Gcd(result, rightCopy);

            if (rightCopyFromPool != null)
                ArrayPool<uint>.Shared.Return(rightCopyFromPool);
        }

        private static void Gcd(Span<uint> left, Span<uint> right)
        {
            Debug.Assert(left.Length >= 2);
            Debug.Assert(right.Length >= 2);
            Debug.Assert(left.Length >= right.Length);

            Span<uint> result = left;   //keep result buffer untouched during computation

            // Executes Lehmer's gcd algorithm, but uses the most
            // significant bits to work with 64-bit (not 32-bit) values.
            // Furthermore we're using an optimized version due to Jebelean.

            // http://cacr.uwaterloo.ca/hac/about/chap14.pdf (see 14.4.2)
            // ftp://ftp.risc.uni-linz.ac.at/pub/techreports/1992/92-69.ps.gz

            while (right.Length > 2)
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

                    Span<uint> temp = left;
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
                        Span<uint> temp = left;
                        left = right;
                        right = temp;
                    }
                }
            }

            if (right.Length > 0)
            {
                // Euclid's step
                Reduce(left, right);

                ulong x = right[0];
                ulong y = left[0];

                if (right.Length > 1)
                {
                    x |= (ulong)right[1] << 32;
                    y |= (ulong)left[1] << 32;
                }

                left = left.Slice(0, Overwrite(left, Gcd(x, y)));
                right.Clear();
            }

            left.CopyTo(result);
        }

        private static int Overwrite(Span<uint> buffer, ulong value)
        {
            Debug.Assert(buffer.Length >= 2);

            if (buffer.Length > 2)
            {
                // Ensure leading zeros in little-endian
                buffer.Slice(2).Clear();
            }

            uint lo = unchecked((uint)value);
            uint hi = (uint)(value >> 32);

            buffer[1] = hi;
            buffer[0] = lo;
            return hi != 0 ? 2 : lo != 0 ? 1 : 0;
        }

        private static void ExtractDigits(ReadOnlySpan<uint> xBuffer,
                                          ReadOnlySpan<uint> yBuffer,
                                          out ulong x, out ulong y)
        {
            Debug.Assert(xBuffer.Length >= 3);
            Debug.Assert(yBuffer.Length >= 3);
            Debug.Assert(xBuffer.Length >= yBuffer.Length);

            // Extracts the most significant bits of x and y,
            // but ensures the quotient x / y does not change!

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

            Debug.Assert(x >= y);
        }

        private static int LehmerCore(Span<uint> x,
                                      Span<uint> y,
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

            return length;
        }

        private static int Refresh(Span<uint> bits, int maxLength)
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
