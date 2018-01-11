// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Buffers.Text;

#if !netstandard
using Internal.Runtime.CompilerServices;
#else
using System.Runtime.CompilerServices;
#endif

//
// This code is copied almost verbatim from the same-named file in CoreRT with mechanical changes to Span-ify it.
//

namespace System
{
    internal static partial class Number
    {
        //
        // Convert a Number to a double.
        //
        internal static bool NumberBufferToDouble(ref NumberBuffer number, out double value)
        {
            double d = NumberToDouble(ref number);

            uint e = DoubleHelper.Exponent(d);
            ulong m = DoubleHelper.Mantissa(d);
            if (e == 0x7FF)
            {
                value = default;
                return false;
            }

            if (e == 0 && m == 0)
            {
                d = 0;
            }

            value = d;

            return true;
        }

        public static unsafe bool NumberBufferToDecimal(ref NumberBuffer number, ref decimal value)
        {
            MutableDecimal d = new MutableDecimal();

            byte* p = number.UnsafeDigits;
            int e = number.Scale;
            if (*p == 0)
            {
                // To avoid risking an app-compat issue with pre 4.5 (where some app was illegally using Reflection to examine the internal scale bits), we'll only force
                // the scale to 0 if the scale was previously positive (previously, such cases were unparsable to a bug.)
                if (e > 0)
                {
                    e = 0;
                }
            }
            else
            {
                if (e > DECIMAL_PRECISION)
                    return false;

                while (((e > 0) || ((*p != 0) && (e > -28))) &&
                       ((d.High < 0x19999999) || ((d.High == 0x19999999) &&
                                                  ((d.Mid < 0x99999999) || ((d.Mid == 0x99999999) &&
                                                                            ((d.Low < 0x99999999) || ((d.Low == 0x99999999) &&
                                                                                                      (*p <= '5'))))))))
                {
                    DecimalDecCalc.DecMul10(ref d);
                    if (*p != 0)
                        DecimalDecCalc.DecAddInt32(ref d, (uint)(*p++ - '0'));
                    e--;
                }

                if (*p++ >= '5')
                {
                    bool round = true;
                    if ((*(p - 1) == '5') && ((*(p - 2) % 2) == 0))
                    {
                        // Check if previous digit is even, only if the when we are unsure whether hows to do
                        // Banker's rounding. For digits > 5 we will be rounding up anyway.
                        int count = 20; // Look at the next 20 digits to check to round
                        while ((*p == '0') && (count != 0))
                        {
                            p++;
                            count--;
                        }
                        if ((*p == '\0') || (count == 0))
                            round = false;// Do nothing
                    }

                    if (round)
                    {
                        DecimalDecCalc.DecAddInt32(ref d, 1);
                        if ((d.High | d.Mid | d.Low) == 0)
                        {
                            // If we got here, the magnitude portion overflowed and wrapped back to 0 as the magnitude was already at the MaxValue point:
                            //
                            //     79,228,162,514,264,337,593,543,950,335e+X
                            //
                            // Manually force it to the correct result:
                            //
                            //      7,922,816,251,426,433,759,354,395,034e+(X+1)
                            //
                            // This code path can be reached by trying to parse the following as a Decimal:
                            //
                            //      0.792281625142643375935439503355e28
                            //

                            d.High = 0x19999999;
                            d.Mid = 0x99999999;
                            d.Low = 0x9999999A;
                            e++;
                        }
                    }
                }
            }

            if (e > 0)
                return false; // Rounding may have caused its own overflow. For example, parsing "0.792281625142643375935439503355e29" will get here.

            if (e <= -DECIMAL_PRECISION)
            {
                // Parsing a large scale zero can give you more precision than fits in the decimal.
                // This should only happen for actual zeros or very small numbers that round to zero.
                d.High = 0;
                d.Low = 0;
                d.Mid = 0;
                d.Scale = DECIMAL_PRECISION - 1;
            }
            else
            {
                d.Scale = -e;
            }
            d.IsNegative = number.IsNegative;

            value = Unsafe.As<MutableDecimal, decimal>(ref d);
            return true;
        }

        public static void DecimalToNumber(decimal value, ref NumberBuffer number)
        {
            ref MutableDecimal d = ref Unsafe.As<decimal, MutableDecimal>(ref value);

            Span<byte> buffer = number.Digits;
            number.IsNegative = d.IsNegative;

            int index = DECIMAL_PRECISION;

            // Starting from the least significant bits, carve off nine decimal digits at a time and string-ize them (using the end of the
            // buffer as a scratch buffer.)
            while (d.Mid != 0 | d.High != 0)
            {
                uint modulo1E9 = DecimalDecCalc.DecDivMod1E9(ref d);
                for (int digitCount = 0; digitCount < 9; digitCount++)
                {
                    buffer[--index] = (byte)(modulo1E9 % 10 + '0');
                    modulo1E9 /= 10;
                }
            }

            // We've finally whittled the decimal down to uint.MaxValue or less. Write the remaining digits but make sure no leading zeros get written.
            uint remainder = d.Low;
            while (remainder != 0)
            {
                buffer[--index] = (byte)(remainder % 10 + '0');
                remainder /= 10;
            }

            int i = DECIMAL_PRECISION - index;
            number.Scale = i - d.Scale;

            // Move the result from the end of the buffer to the beginning where we need it.
            Span<byte> dst = number.Digits;
            int dstIndex = 0;
            while (--i >= 0)
            {
                dst[dstIndex++] = buffer[index++];
            }
            dst[dstIndex] = 0;

            number.CheckConsistency();
        }

        //
        // get 32-bit integer from at most 9 digits
        //
        private static uint DigitsToInt(ReadOnlySpan<byte> digits, int count)
        {
            bool success = Utf8Parser.TryParse(digits.Slice(0, count), out uint value, out int bytesConsumed, 'D');
            Debug.Assert(success); // This is only called on the contents of a trusted Number structure.
            return value;
        }

        //
        // helper to multiply two 32-bit uints
        //
        private static ulong Mul32x32To64(uint a, uint b)
        {
            return a * (ulong)b;
        }

        //
        // multiply two numbers in the internal integer representation
        //
        private static ulong Mul64Lossy(ulong a, ulong b, ref int pexp)
        {
            // it's ok to lose some precision here - Mul64 will be called
            // at most twice during the conversion, so the error won't propagate
            // to any of the 53 significant bits of the result
            ulong val = Mul32x32To64((uint)(a >> 32), (uint)(b >> 32)) +
                (Mul32x32To64((uint)(a >> 32), (uint)(b)) >> 32) +
                (Mul32x32To64((uint)(a), (uint)(b >> 32)) >> 32);

            // normalize
            if ((val & 0x8000000000000000) == 0)
            {
                val <<= 1;
                pexp -= 1;
            }

            return val;
        }

        //
        // precomputed tables with powers of 10. These allows us to do at most
        // two Mul64 during the conversion. This is important not only
        // for speed, but also for precision because of Mul64 computes with 1 bit error.
        //

        private static readonly ulong[] s_rgval64Power10 =
        {
            // powers of 10
            /*1*/ 0xa000000000000000,
            /*2*/ 0xc800000000000000,
            /*3*/ 0xfa00000000000000,
            /*4*/ 0x9c40000000000000,
            /*5*/ 0xc350000000000000,
            /*6*/ 0xf424000000000000,
            /*7*/ 0x9896800000000000,
            /*8*/ 0xbebc200000000000,
            /*9*/ 0xee6b280000000000,
            /*10*/ 0x9502f90000000000,
            /*11*/ 0xba43b74000000000,
            /*12*/ 0xe8d4a51000000000,
            /*13*/ 0x9184e72a00000000,
            /*14*/ 0xb5e620f480000000,
            /*15*/ 0xe35fa931a0000000,

            // powers of 0.1
            /*1*/ 0xcccccccccccccccd,
            /*2*/ 0xa3d70a3d70a3d70b,
            /*3*/ 0x83126e978d4fdf3c,
            /*4*/ 0xd1b71758e219652e,
            /*5*/ 0xa7c5ac471b478425,
            /*6*/ 0x8637bd05af6c69b7,
            /*7*/ 0xd6bf94d5e57a42be,
            /*8*/ 0xabcc77118461ceff,
            /*9*/ 0x89705f4136b4a599,
            /*10*/ 0xdbe6fecebdedd5c2,
            /*11*/ 0xafebff0bcb24ab02,
            /*12*/ 0x8cbccc096f5088cf,
            /*13*/ 0xe12e13424bb40e18,
            /*14*/ 0xb424dc35095cd813,
            /*15*/ 0x901d7cf73ab0acdc,
        };

        private static readonly sbyte[] s_rgexp64Power10 =
        {
            // exponents for both powers of 10 and 0.1
            /*1*/ 4,
            /*2*/ 7,
            /*3*/ 10,
            /*4*/ 14,
            /*5*/ 17,
            /*6*/ 20,
            /*7*/ 24,
            /*8*/ 27,
            /*9*/ 30,
            /*10*/ 34,
            /*11*/ 37,
            /*12*/ 40,
            /*13*/ 44,
            /*14*/ 47,
            /*15*/ 50,
        };

        private static readonly ulong[] s_rgval64Power10By16 =
        {
            // powers of 10^16
            /*1*/ 0x8e1bc9bf04000000,
            /*2*/ 0x9dc5ada82b70b59e,
            /*3*/ 0xaf298d050e4395d6,
            /*4*/ 0xc2781f49ffcfa6d4,
            /*5*/ 0xd7e77a8f87daf7fa,
            /*6*/ 0xefb3ab16c59b14a0,
            /*7*/ 0x850fadc09923329c,
            /*8*/ 0x93ba47c980e98cde,
            /*9*/ 0xa402b9c5a8d3a6e6,
            /*10*/ 0xb616a12b7fe617a8,
            /*11*/ 0xca28a291859bbf90,
            /*12*/ 0xe070f78d39275566,
            /*13*/ 0xf92e0c3537826140,
            /*14*/ 0x8a5296ffe33cc92c,
            /*15*/ 0x9991a6f3d6bf1762,
            /*16*/ 0xaa7eebfb9df9de8a,
            /*17*/ 0xbd49d14aa79dbc7e,
            /*18*/ 0xd226fc195c6a2f88,
            /*19*/ 0xe950df20247c83f8,
            /*20*/ 0x81842f29f2cce373,
            /*21*/ 0x8fcac257558ee4e2,

            // powers of 0.1^16
            /*1*/ 0xe69594bec44de160,
            /*2*/ 0xcfb11ead453994c3,
            /*3*/ 0xbb127c53b17ec165,
            /*4*/ 0xa87fea27a539e9b3,
            /*5*/ 0x97c560ba6b0919b5,
            /*6*/ 0x88b402f7fd7553ab,
            /*7*/ 0xf64335bcf065d3a0,
            /*8*/ 0xddd0467c64bce4c4,
            /*9*/ 0xc7caba6e7c5382ed,
            /*10*/ 0xb3f4e093db73a0b7,
            /*11*/ 0xa21727db38cb0053,
            /*12*/ 0x91ff83775423cc29,
            /*13*/ 0x8380dea93da4bc82,
            /*14*/ 0xece53cec4a314f00,
            /*15*/ 0xd5605fcdcf32e217,
            /*16*/ 0xc0314325637a1978,
            /*17*/ 0xad1c8eab5ee43ba2,
            /*18*/ 0x9becce62836ac5b0,
            /*19*/ 0x8c71dcd9ba0b495c,
            /*20*/ 0xfd00b89747823938,
            /*21*/ 0xe3e27a444d8d991a,
        };

        private static readonly short[] s_rgexp64Power10By16 =
        {
            // exponents for both powers of 10^16 and 0.1^16
            /*1*/ 54,
            /*2*/ 107,
            /*3*/ 160,
            /*4*/ 213,
            /*5*/ 266,
            /*6*/ 319,
            /*7*/ 373,
            /*8*/ 426,
            /*9*/ 479,
            /*10*/ 532,
            /*11*/ 585,
            /*12*/ 638,
            /*13*/ 691,
            /*14*/ 745,
            /*15*/ 798,
            /*16*/ 851,
            /*17*/ 904,
            /*18*/ 957,
            /*19*/ 1010,
            /*20*/ 1064,
            /*21*/ 1117,
        };

        private static int abs(int value)
        {
            if (value < 0)
                return -value;
            return value;
        }

        private static unsafe double NumberToDouble(ref NumberBuffer number)
        {
            ulong val;
            int exp;
            ReadOnlySpan<byte> src = number.Digits;
            int remaining;
            int total;
            int count;
            int scale;
            int absscale;
            int index;
            int srcIndex = 0;

            total = number.NumDigits;
            remaining = total;

            // skip the leading zeros
            while (src[srcIndex] == '0')
            {
                remaining--;
                srcIndex++;
            }

            if (remaining == 0)
                return 0;

            count = Math.Min(remaining, 9);
            remaining -= count;
            val = DigitsToInt(src, count);

            if (remaining > 0)
            {
                count = Math.Min(remaining, 9);
                remaining -= count;

                // get the denormalized power of 10
                uint mult = (uint)(s_rgval64Power10[count - 1] >> (64 - s_rgexp64Power10[count - 1]));
                val = Mul32x32To64((uint)val, mult) + DigitsToInt(src.Slice(9), count);
            }

            scale = number.Scale - (total - remaining);
            absscale = abs(scale);
            if (absscale >= 22 * 16)
            {
                // overflow / underflow
                ulong result = (scale > 0) ? 0x7FF0000000000000 : 0ul;
                if (number.IsNegative)
                    result |= 0x8000000000000000;
                return *(double*)&result;
            }

            exp = 64;

            // normalize the mantissa
            if ((val & 0xFFFFFFFF00000000) == 0)
            { val <<= 32; exp -= 32; }
            if ((val & 0xFFFF000000000000) == 0)
            { val <<= 16; exp -= 16; }
            if ((val & 0xFF00000000000000) == 0)
            { val <<= 8; exp -= 8; }
            if ((val & 0xF000000000000000) == 0)
            { val <<= 4; exp -= 4; }
            if ((val & 0xC000000000000000) == 0)
            { val <<= 2; exp -= 2; }
            if ((val & 0x8000000000000000) == 0)
            { val <<= 1; exp -= 1; }

            index = absscale & 15;
            if (index != 0)
            {
                int multexp = s_rgexp64Power10[index - 1];
                // the exponents are shared between the inverted and regular table
                exp += (scale < 0) ? (-multexp + 1) : multexp;

                ulong multval = s_rgval64Power10[index + ((scale < 0) ? 15 : 0) - 1];
                val = Mul64Lossy(val, multval, ref exp);
            }

            index = absscale >> 4;
            if (index != 0)
            {
                int multexp = s_rgexp64Power10By16[index - 1];
                // the exponents are shared between the inverted and regular table
                exp += (scale < 0) ? (-multexp + 1) : multexp;

                ulong multval = s_rgval64Power10By16[index + ((scale < 0) ? 21 : 0) - 1];
                val = Mul64Lossy(val, multval, ref exp);
            }

            // round & scale down
            if (((int)val & (1 << 10)) != 0)
            {
                // IEEE round to even
                ulong tmp = val + ((1 << 10) - 1) + (ulong)(((int)val >> 11) & 1);
                if (tmp < val)
                {
                    // overflow
                    tmp = (tmp >> 1) | 0x8000000000000000;
                    exp += 1;
                }
                val = tmp;
            }

            // return the exponent to a biased state
            exp += 0x3FE;

            // handle overflow, underflow, "Epsilon - 1/2 Epsilon", denormalized, and the normal case
            if (exp <= 0)
            {
                if (exp == -52 && (val >= 0x8000000000000058))
                {
                    // round X where {Epsilon > X >= 2.470328229206232730000000E-324} up to Epsilon (instead of down to zero)
                    val = 0x0000000000000001;
                }
                else if (exp <= -52)
                {
                    // underflow
                    val = 0;
                }
                else
                {
                    // denormalized
                    val >>= (-exp + 11 + 1);
                }
            }
            else if (exp >= 0x7FF)
            {
                // overflow
                val = 0x7FF0000000000000;
            }
            else
            {
                // normal postive exponent case
                val = ((ulong)exp << 52) + ((val >> 11) & 0x000FFFFFFFFFFFFF);
            }

            if (number.IsNegative)
                val |= 0x8000000000000000;

            return *(double*)&val;
        }

        private static class DoubleHelper
        {
            public static unsafe uint Exponent(double d)
            {
                return (*((uint*)&d + 1) >> 20) & 0x000007ff;
            }

            public static unsafe ulong Mantissa(double d)
            {
                return (*((uint*)&d)) | ((ulong)(*((uint*)&d + 1) & 0x000fffff) << 32);
            }

            public static unsafe bool Sign(double d)
            {
                return (*((uint*)&d + 1) >> 31) != 0;
            }
        }
    }
}
