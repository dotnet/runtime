// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System
{
    internal static partial class Number
    {
        internal static unsafe class Grisu3
        {
            private const int Alpha = -59;
            private const double D1Log210 = 0.301029995663981195;
            private const int Gamma = -32;
            private const int PowerDecimalExponentDistance = 8;
            private const int PowerMinDecimalExponent = -348;
            private const int PowerMaxDecimalExponent = 340;
            private const int PowerOffset = -PowerMinDecimalExponent;
            private const uint Ten4 = 10000;
            private const uint Ten5 = 100000;
            private const uint Ten6 = 1000000;
            private const uint Ten7 = 10000000;
            private const uint Ten8 = 100000000;
            private const uint Ten9 = 1000000000;

            private static readonly short[] s_CachedPowerBinaryExponents = new short[]
            {
                -1220,
                -1193,
                -1166,
                -1140,
                -1113,
                -1087,
                -1060,
                -1034,
                -1007,
                -980,
                -954,
                -927,
                -901,
                -874,
                -847,
                -821,
                -794,
                -768,
                -741,
                -715,
                -688,
                -661,
                -635,
                -608,
                -582,
                -555,
                -529,
                -502,
                -475,
                -449,
                -422,
                -396,
                -369,
                -343,
                -316,
                -289,
                -263,
                -236,
                -210,
                -183,
                -157,
                -130,
                -103,
                -77,
                -50,
                -24,
                3,
                30,
                56,
                83,
                109,
                136,
                162,
                189,
                216,
                242,
                269,
                295,
                322,
                348,
                375,
                402,
                428,
                455,
                481,
                508,
                534,
                561,
                588,
                614,
                641,
                667,
                694,
                720,
                747,
                774,
                800,
                827,
                853,
                880,
                907,
                933,
                960,
                986,
                1013,
                1039,
                1066,
            };

            private static readonly short[] s_CachedPowerDecimalExponents = new short[]
            {
                PowerMinDecimalExponent,
                -340,
                -332,
                -324,
                -316,
                -308,
                -300,
                -292,
                -284,
                -276,
                -268,
                -260,
                -252,
                -244,
                -236,
                -228,
                -220,
                -212,
                -204,
                -196,
                -188,
                -180,
                -172,
                -164,
                -156,
                -148,
                -140,
                -132,
                -124,
                -116,
                -108,
                -100,
                -92,
                -84,
                -76,
                -68,
                -60,
                -52,
                -44,
                -36,
                -28,
                -20,
                -12,
                -4,
                4,
                12,
                20,
                28,
                36,
                44,
                52,
                60,
                68,
                76,
                84,
                92,
                100,
                108,
                116,
                124,
                132,
                140,
                148,
                156,
                164,
                172,
                180,
                188,
                196,
                204,
                212,
                220,
                228,
                236,
                244,
                252,
                260,
                268,
                276,
                284,
                292,
                300,
                308,
                316,
                324,
                332,
                PowerMaxDecimalExponent,
            };

            private static readonly uint[] s_CachedPowerOfTen = new uint[]
            {
                1,          // 10^0
                10,         // 10^1
                100,        // 10^2
                1000,       // 10^3
                10000,      // 10^4
                100000,     // 10^5
                1000000,    // 10^6
                10000000,   // 10^7
                100000000,  // 10^8
                1000000000, // 10^9
            };

            private static readonly ulong[] s_CachedPowerSignificands = new ulong[]
            {
                0xFA8FD5A0081C0288,
                0xBAAEE17FA23EBF76,
                0x8B16FB203055AC76,
                0xCF42894A5DCE35EA,
                0x9A6BB0AA55653B2D,
                0xE61ACF033D1A45DF,
                0xAB70FE17C79AC6CA,
                0xFF77B1FCBEBCDC4F,
                0xBE5691EF416BD60C,
                0x8DD01FAD907FFC3C,
                0xD3515C2831559A83,
                0x9D71AC8FADA6C9B5,
                0xEA9C227723EE8BCB,
                0xAECC49914078536D,
                0x823C12795DB6CE57,
                0xC21094364DFB5637,
                0x9096EA6F3848984F,
                0xD77485CB25823AC7,
                0xA086CFCD97BF97F4,
                0xEF340A98172AACE5,
                0xB23867FB2A35B28E,
                0x84C8D4DFD2C63F3B,
                0xC5DD44271AD3CDBA,
                0x936B9FCEBB25C996,
                0xDBAC6C247D62A584,
                0xA3AB66580D5FDAF6,
                0xF3E2F893DEC3F126,
                0xB5B5ADA8AAFF80B8,
                0x87625F056C7C4A8B,
                0xC9BCFF6034C13053,
                0x964E858C91BA2655,
                0xDFF9772470297EBD,
                0xA6DFBD9FB8E5B88F,
                0xF8A95FCF88747D94,
                0xB94470938FA89BCF,
                0x8A08F0F8BF0F156B,
                0xCDB02555653131B6,
                0x993FE2C6D07B7FAC,
                0xE45C10C42A2B3B06,
                0xAA242499697392D3,
                0xFD87B5F28300CA0E,
                0xBCE5086492111AEB,
                0x8CBCCC096F5088CC,
                0xD1B71758E219652C,
                0x9C40000000000000,
                0xE8D4A51000000000,
                0xAD78EBC5AC620000,
                0x813F3978F8940984,
                0xC097CE7BC90715B3,
                0x8F7E32CE7BEA5C70,
                0xD5D238A4ABE98068,
                0x9F4F2726179A2245,
                0xED63A231D4C4FB27,
                0xB0DE65388CC8ADA8,
                0x83C7088E1AAB65DB,
                0xC45D1DF942711D9A,
                0x924D692CA61BE758,
                0xDA01EE641A708DEA,
                0xA26DA3999AEF774A,
                0xF209787BB47D6B85,
                0xB454E4A179DD1877,
                0x865B86925B9BC5C2,
                0xC83553C5C8965D3D,
                0x952AB45CFA97A0B3,
                0xDE469FBD99A05FE3,
                0xA59BC234DB398C25,
                0xF6C69A72A3989F5C,
                0xB7DCBF5354E9BECE,
                0x88FCF317F22241E2,
                0xCC20CE9BD35C78A5,
                0x98165AF37B2153DF,
                0xE2A0B5DC971F303A,
                0xA8D9D1535CE3B396,
                0xFB9B7CD9A4A7443C,
                0xBB764C4CA7A44410,
                0x8BAB8EEFB6409C1A,
                0xD01FEF10A657842C,
                0x9B10A4E5E9913129,
                0xE7109BFBA19C0C9D,
                0xAC2820D9623BF429,
                0x80444B5E7AA7CF85,
                0xBF21E44003ACDD2D,
                0x8E679C2F5E44FF8F,
                0xD433179D9C8CB841,
                0x9E19DB92B4E31BA9,
                0xEB96BF6EBADF77D9,
                0xAF87023B9BF0EE6B,
            };

            public static bool Run(double value, int precision, ref NumberBuffer number)
            {
                // ========================================================================================================================================
                // This implementation is based on the paper: http://www.cs.tufts.edu/~nr/cs257/archive/florian-loitsch/printf.pdf
                // You must read this paper to fully understand the code.
                //
                // Deviation: Instead of generating shortest digits, we generate the digits according to the input count.
                // Therefore, we do not need m+ and m- which are used to determine the exact range of values.
                // ======================================================================================================================================== 
                //
                // Overview:
                //
                // The idea of Grisu3 is to leverage additional bits and cached power of ten to produce the digits.
                // We need to create a handmade floating point data structure DiyFp to extend the bits of double.
                // We also need to cache the powers of ten for digits generation. By choosing the correct index of powers
                // we need to start with, we can eliminate the expensive big num divide operation.
                //
                // Grisu3 is imprecision for some numbers. Fortunately, the algorithm itself can determine that and give us
                // a success/fail flag. We may fall back to other algorithms (For instance, Dragon4) if it fails.
                //
                // w: the normalized DiyFp from the input value.
                // mk: The index of the cached powers.
                // cmk: The cached power.
                // D: Product: w * cmk.
                // kappa: A factor used for generating digits. See step 5 of the Grisu3 procedure in the paper.

                // Handle sign bit.
                if (double.IsNegative(value))
                {
                    value = -value;
                    number.IsNegative = true;
                }
                else
                {
                    number.IsNegative = false;
                }

                // Step 1: Determine the normalized DiyFp w.

                DiyFp.GenerateNormalizedDiyFp(value, out DiyFp w);

                // Step 2: Find the cached power of ten.

                // Compute the proper index mk.
                int mk = KComp(w.e + DiyFp.SignificandLength);

                // Retrieve the cached power of ten.
                CachedPower(mk, out DiyFp cmk, out int decimalExponent);

                // Step 3: Scale the w with the cached power of ten.

                DiyFp.Multiply(ref w, ref cmk, out DiyFp D);

                // Step 4: Generate digits.

                bool isSuccess = DigitGen(ref D, precision, number.GetDigitsPointer(), out int length, out int kappa);

                if (isSuccess)
                {
                    number.Digits[precision] = (byte)('\0');
                    number.Scale = (length - decimalExponent + kappa);
                }

                return isSuccess;
            }

            // Returns the biggest power of ten that is less than or equal to the given number.
            static void BiggestPowerTenLessThanOrEqualTo(uint number, int bits, out uint power, out int exponent)
            {
                switch (bits)
                {
                    case 32:
                    case 31:
                    case 30:
                    {
                        if (Ten9 <= number)
                        {
                            power = Ten9;
                            exponent = 9;
                            break;
                        }

                        goto case 29;
                    }

                    case 29:
                    case 28:
                    case 27:
                    {
                        if (Ten8 <= number)
                        {
                            power = Ten8;
                            exponent = 8;
                            break;
                        }

                        goto case 26;
                    }

                    case 26:
                    case 25:
                    case 24:
                    {
                        if (Ten7 <= number)
                        {
                            power = Ten7;
                            exponent = 7;
                            break;
                        }

                        goto case 23;
                    }

                    case 23:
                    case 22:
                    case 21:
                    case 20:
                    {
                        if (Ten6 <= number)
                        {
                            power = Ten6;
                            exponent = 6;
                            break;
                        }

                        goto case 19;
                    }

                    case 19:
                    case 18:
                    case 17:
                    {
                        if (Ten5 <= number)
                        {
                            power = Ten5;
                            exponent = 5;
                            break;
                        }

                        goto case 16;
                    }

                    case 16:
                    case 15:
                    case 14:
                    {
                        if (Ten4 <= number)
                        {
                            power = Ten4;
                            exponent = 4;
                            break;
                        }

                        goto case 13;
                    }

                    case 13:
                    case 12:
                    case 11:
                    case 10:
                    {
                        if (1000 <= number)
                        {
                            power = 1000;
                            exponent = 3;
                            break;
                        }

                        goto case 9;
                    }

                    case 9:
                    case 8:
                    case 7:
                    {
                        if (100 <= number)
                        {
                            power = 100;
                            exponent = 2;
                            break;
                        }

                        goto case 6;
                    }

                    case 6:
                    case 5:
                    case 4:
                    {
                        if (10 <= number)
                        {
                            power = 10;
                            exponent = 1;
                            break;
                        }

                        goto case 3;
                    }

                    case 3:
                    case 2:
                    case 1:
                    {
                        if (1 <= number)
                        {
                            power = 1;
                            exponent = 0;
                            break;
                        }

                        goto case 0;
                    }

                    case 0:
                    {
                        power = 0;
                        exponent = -1;
                        break;
                    }

                    default:
                    {
                        power = 0;
                        exponent = 0;

                        Debug.Fail("unreachable");
                        break;
                    }
                }
            }

            private static void CachedPower(int k, out DiyFp cmk, out int decimalExponent)
            {
                int index = ((PowerOffset + k - 1) / PowerDecimalExponentDistance) + 1;
                cmk = new DiyFp(s_CachedPowerSignificands[index], s_CachedPowerBinaryExponents[index]);
                decimalExponent = s_CachedPowerDecimalExponents[index];
            }

            private static bool DigitGen(ref DiyFp mp, int precision, byte* digits, out int length, out int k)
            {
                // Split the input mp to two parts. Part 1 is integral. Part 2 can be used to calculate
                // fractional.
                //
                // mp: the input DiyFp scaled by cached power.
                // K: final kappa.
                // p1: part 1.
                // p2: part 2.

                Debug.Assert(precision > 0);
                Debug.Assert(digits != null);
                Debug.Assert(mp.e >= Alpha);
                Debug.Assert(mp.e <= Gamma);

                ulong mpF = mp.f;
                int mpE = mp.e;

                var one = new DiyFp(1UL << -mpE, mpE);

                ulong oneF = one.f;
                int oneNegE = -one.e;

                ulong ulp = 1;

                uint p1 = (uint)(mpF >> oneNegE);
                ulong p2 = mpF & (oneF - 1);

                // When p2 (fractional part) is zero, we can predicate if p1 is good to produce the numbers in requested digit count:
                //
                // - When requested digit count >= 11, p1 is not be able to exhaust the count as 10^(11 - 1) > uint.MaxValue >= p1.
                // - When p1 < 10^(count - 1), p1 is not be able to exhaust the count.
                // - Otherwise, p1 may have chance to exhaust the count.
                if ((p2 == 0) && ((precision >= 11) || (p1 < s_CachedPowerOfTen[precision - 1])))
                {
                    length = 0;
                    k = 0;
                    return false;
                }

                // Note: The code in the paper simply assigns div to Ten9 and kappa to 10 directly.
                // That means we need to check if any leading zero of the generated
                // digits during the while loop, which hurts the performance.
                //
                // Now if we can estimate what the div and kappa, we do not need to check the leading zeros.
                // The idea is to find the biggest power of 10 that is less than or equal to the given number.
                // Then we don't need to worry about the leading zeros and we can get 10% performance gain.
                int index = 0;
                BiggestPowerTenLessThanOrEqualTo(p1, (DiyFp.SignificandLength - oneNegE), out uint div, out int kappa);
                kappa++;

                // Produce integral.
                while (kappa > 0)
                {
                    int d = (int)(Math.DivRem(p1, div, out p1));
                    digits[index] = (byte)('0' + d);

                    index++;
                    precision--;
                    kappa--;

                    if (precision == 0)
                    {
                        break;
                    }

                    div /= 10;
                }

                // End up here if we already exhausted the digit count.
                if (precision == 0)
                {
                    ulong rest = ((ulong)(p1) << oneNegE) + p2;

                    length = index;
                    k = kappa;

                    return RoundWeed(
                        digits,
                        index,
                        rest,
                        ((ulong)(div)) << oneNegE,
                        ulp,
                        ref k
                    );
                }

                // We have to generate digits from part2 if we have requested digit count left
                // and part2 is greater than ulp.
                while ((precision > 0) && (p2 > ulp))
                {
                    p2 *= 10;

                    int d = (int)(p2 >> oneNegE);
                    digits[index] = (byte)('0' + d);

                    index++;
                    precision--;
                    kappa--;

                    p2 &= (oneF - 1);

                    ulp *= 10;
                }

                // If we haven't exhausted the requested digit counts, the Grisu3 algorithm fails.
                if (precision != 0)
                {
                    length = 0;
                    k = 0;
                    return false;
                }

                length = index;
                k = kappa;

                return RoundWeed(digits, index, p2, oneF, ulp, ref k);
            }

            private static int KComp(int e)
            {
                return (int)(Math.Ceiling((Alpha - e + DiyFp.SignificandLength - 1) * D1Log210));
            }

            private static bool RoundWeed(byte* buffer, int len, ulong rest, ulong tenKappa, ulong ulp, ref int kappa)
            {
                Debug.Assert(rest < tenKappa);

                // 1. tenKappa <= ulp: we don't have an idea which way to round.
                // 2. Even if tenKappa > ulp, but if tenKappa <= 2 * ulp we cannot find the way to round.
                // Note: to prevent overflow, we need to use tenKappa - ulp <= ulp.
                if ((tenKappa <= ulp) || ((tenKappa - ulp) <= ulp))
                {
                    return false;
                }

                // tenKappa >= 2 * (rest + ulp). We should round down.
                // Note: to prevent overflow, we need to check if tenKappa > 2 * rest as a prerequisite.
                if (((tenKappa - rest) > rest) && ((tenKappa - (2 * rest)) >= (2 * ulp)))
                {
                    return true;
                }

                // tenKappa <= 2 * (rest - ulp). We should round up.
                // Note: to prevent overflow, we need to check if rest > ulp as a prerequisite.
                if ((rest > ulp) && (tenKappa <= (rest - ulp) || ((tenKappa - (rest - ulp)) <= (rest - ulp))))
                {
                    // Find all 9s from end to start.
                    buffer[len - 1]++;
                    for (int i = len - 1; i > 0; i--)
                    {
                        if (buffer[i] != (byte)('0' + 10))
                        {
                            // We end up a number less than 9.
                            break;
                        }

                        // Current number becomes 0 and add the promotion to the next number.
                        buffer[i] = (byte)('0');
                        buffer[i - 1]++;
                    }

                    if (buffer[0] == (char)('0' + 10))
                    {
                        // First number is '0' + 10 means all numbers are 9.
                        // We simply make the first number to 1 and increase the kappa.
                        buffer[0] = (byte)('1');
                        kappa++;
                    }

                    return true;
                }

                return false;
            }
        }
    }
}
