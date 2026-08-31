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
            Debug.Assert(left.Length >= 2);
            Debug.Assert(right.Length >= 2);
            Debug.Assert(CompareActual(left, right) >= 0);
            Debug.Assert(result.Length == left.Length);

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
