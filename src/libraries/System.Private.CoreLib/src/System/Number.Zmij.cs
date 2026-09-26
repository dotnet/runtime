// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    // This is a port of the Zmij shortest float-to-decimal conversion by Victor Zverovich
    // (https://github.com/vitaut/zmij), via the C# implementations Zmij.NET and ZmijSharp
    // (https://github.com/nuskey8/Zmij.NET, https://github.com/akeit0/ZmijSharp). See THIRD-PARTY-NOTICES.TXT.
    //
    // A value is scaled by one 128-bit power of ten so that the integer part holds all but the last
    // candidate digit; the fractional part decides rounding and whether that last digit is needed.
    // The 128-bit significands of 10^-307..10^341 are stored directly (10.4 KB); vitaut/zmij and
    // ZmijSharp also have a 670-byte reconstructed cache that costs about 10 % on this path. Bounded
    // precision (1..18 significant digits, ties to even) uses the same table; Dragon4 remains for more.
    internal static partial class Number
    {
        internal static class Zmij
        {
            private const int DoubleSignificandBits = 52;
            private const int DoubleExponentOffset = 1023 + DoubleSignificandBits;
            private const ulong DoubleImplicitBit = 1UL << DoubleSignificandBits;

            private const int ExtraShift = 6;
            private const ulong BiasedHalf = 0x8000_0000_0000_0006UL;

            private const int MinCacheExponent = -307;
            private const int MaxCacheExponent = 341;

            // Produces the shortest round-trippable digits for a finite, non-zero double, float, Half or BFloat16.
            // Returns false for other types.
            public static bool TryRun<TNumber>(TNumber value, ref NumberBuffer number)
                where TNumber : unmanaged, IBinaryFloatParseAndFormatInfo<TNumber>
            {
                ulong bits = TNumber.FloatToBits(value);
                ulong significand;
                int exponent;

                if (typeof(TNumber) == typeof(double))
                {
                    ToDecimal(bits, out significand, out exponent);
                }
                else if ((typeof(TNumber) == typeof(float)) || (typeof(TNumber) == typeof(Half)) || (typeof(TNumber) == typeof(BFloat16)))
                {
                    ToDecimal((uint)bits, TNumber.DenormalMantissaBits, TNumber.ExponentBias, TNumber.InfinityExponent, out significand, out exponent);
                }
                else
                {
                    return false;
                }

                Debug.Assert(significand != 0);

                // The producer may leave trailing decimal zeros (up to 16 for a value such as 0.1, which it yields as
                // 10^16 * 10^-17); the number buffer wants them in the scale, so peel them in chunks.
                if (significand % 10 == 0)
                {
                    while (significand % 100000000 == 0)
                    {
                        significand /= 100000000;
                        exponent += 8;
                    }
                    if (significand % 10000 == 0)
                    {
                        significand /= 10000;
                        exponent += 4;
                    }
                    if (significand % 100 == 0)
                    {
                        significand /= 100;
                        exponent += 2;
                    }
                    if (significand % 10 == 0)
                    {
                        significand /= 10;
                        exponent += 1;
                    }
                }

                int length = FormattingHelpers.CountDigits(significand);
                Debug.Assert(length <= 17);

                int start = UInt64ToDecChars(number.Digits, length, significand);
                Debug.Assert(start == 0);

                number.Scale = length + exponent;
                number.Digits[length] = (byte)('\0');
                number.DigitsCount = length;
                return true;
            }

            // Produces exactly precision (1..18) significant digits of a finite, non-zero value, correctly rounded
            // with ties to even (the port of write_scientific's fixed-precision path in vitaut/zmij). Returns false
            // when precision is out of that range or the required power of ten is outside the table, in which
            // case the caller falls back to Dragon4.
            public static bool TryRun<TNumber>(TNumber value, int precision, ref NumberBuffer number)
                where TNumber : unmanaged, IBinaryFloatParseAndFormatInfo<TNumber>
            {
                if ((precision < 1) || (precision > MaxPrecision))
                {
                    return false;
                }

                ulong bits = TNumber.FloatToBits(value);
                int biasedExponent = (int)(bits >> TNumber.DenormalMantissaBits) & TNumber.InfinityExponent;
                ulong significand = bits & TNumber.DenormalMantissaMask;
                int exponent;

                Debug.Assert(biasedExponent != TNumber.InfinityExponent);
                Debug.Assert((biasedExponent | (int)(significand != 0 ? 1 : 0)) != 0);

                if (biasedExponent == 0)
                {
                    exponent = 1 - TNumber.ExponentBias - TNumber.DenormalMantissaBits;
                }
                else
                {
                    significand |= 1UL << TNumber.DenormalMantissaBits;
                    exponent = biasedExponent - TNumber.ExponentBias - TNumber.DenormalMantissaBits;
                }

                // Normalize so that value = significand * 2^exponent with bit 63 of the significand set.
                int leadingZeros = BitOperations.LeadingZeroCount(significand);
                significand <<= leadingZeros;
                exponent -= leadingZeros;

                // Scale by 10^-decimalExponent so that the integral part has precision digits; the estimate uses the
                // lower bound of the value's magnitude, so it can come out one digit too long, never too short.
                int decimalExponent = ComputeDecimalExponent(exponent + 63, regular: true) - (precision - 1);
                if ((-decimalExponent < MinCacheExponent) || (-decimalExponent > MaxCacheExponent))
                {
                    return false;
                }

                int pointShift = -ComputeExponentShift(exponent, decimalExponent);
                Debug.Assert((pointShift >= 1) && (pointShift < 64));

                // High 128 bits of the 192-bit product (pow10High:pow10Low + 1) * significand; the +1 turns the
                // rounded-down power into an upper bound so that a truncated product cannot fake an exact tie.
                GetPowerOf10(-decimalExponent, out ulong pow10High, out ulong pow10Low);
                ulong productHigh = Math.BigMul(pow10High, significand, out ulong productLow);
                ulong lowHigh = Math.BigMul(pow10Low + 1, significand, out _);
                productLow += lowHigh;
                productHigh += (productLow < lowHigh) ? 1UL : 0UL;

                ulong integral = productHigh >> pointShift;
                ulong fraction = productHigh << (64 - pointShift);
                ulong fractionTail = productLow;

                const ulong Half = 1UL << 63;
                bool roundUp = (fraction > Half) || ((fraction == Half) && ((fractionTail != 0) || ((integral & 1) != 0)));
                ulong digits = integral + (roundUp ? 1UL : 0UL);

                if (digits >= Read(Pow10, precision))
                {
                    // One digit too many: round one place coarser, the dropped fraction disambiguating a trailing 5.
                    digits = integral / 10;
                    ulong lastDigit = integral - digits * 10;
                    bool hasFraction = (fraction | fractionTail) != 0;
                    roundUp = (lastDigit > 5) || ((lastDigit == 5) && (hasFraction || ((digits & 1) != 0)));
                    digits += roundUp ? 1UL : 0UL;
                    decimalExponent++;
                }

                Debug.Assert((digits >= Read(Pow10, precision - 1)) && (digits < Read(Pow10, precision)));

                int start = UInt64ToDecChars(number.Digits, precision, digits);
                Debug.Assert(start == 0);

                number.Scale = decimalExponent + precision;
                number.Digits[precision] = (byte)('\0');
                number.DigitsCount = precision;
                return true;
            }

            private const int MaxPrecision = 18;

            // 10^0 .. 10^18
            private static ReadOnlySpan<ulong> Pow10 =>
            [
                1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000,
                10000000000, 100000000000, 1000000000000, 10000000000000, 100000000000000,
                1000000000000000, 10000000000000000, 100000000000000000, 1000000000000000000,
            ];

            private static void ToDecimal(ulong bits, out ulong significand, out int exponent)
            {
                int binaryExponent = (int)((bits << 1) >> (DoubleSignificandBits + 1));
                ulong binarySignificand = bits & (DoubleImplicitBit - 1);

                Debug.Assert(binaryExponent != 0x7FF);
                Debug.Assert((binaryExponent | (int)(binarySignificand != 0 ? 1 : 0)) != 0);

                bool regular;
                if (binaryExponent == 0)
                {
                    // Subnormal: no implicit bit and a symmetric rounding interval.
                    binaryExponent = 1;
                    regular = true;
                }
                else
                {
                    // Powers of two (zero fraction) have an asymmetric interval below them.
                    regular = binarySignificand != 0;
                    binarySignificand |= DoubleImplicitBit;

                    // Integers below 2^53 are their own shortest representation; the caller strips trailing zeros.
                    int shift = DoubleExponentOffset - binaryExponent;
                    if (((uint)shift <= DoubleSignificandBits) && ((binarySignificand & ((1UL << shift) - 1)) == 0))
                    {
                        significand = binarySignificand >> shift;
                        exponent = 0;
                        return;
                    }
                }

                ulong integral = ToDecimal(binarySignificand, binaryExponent, regular, out int decimalExponent, out int lastDigit);
                if (lastDigit >= 0)
                {
                    significand = integral * 10 + (uint)lastDigit;
                    exponent = decimalExponent;
                }
                else
                {
                    significand = integral;
                    exponent = decimalExponent + 1;
                }
            }

            // The 32-bit core is parameterized by the format: significandBits explicit bits, exponentBias and the
            // all-ones exponent. It serves float (23, 127, 0xFF), Half (10, 15, 0x1F) and BFloat16 (7, 127, 0xFF).
            private static void ToDecimal(uint bits, int significandBits, int exponentBias, int exponentMask, out ulong significand, out int exponent)
            {
                int binaryExponent = (int)(bits >> significandBits) & exponentMask;
                uint implicitBit = 1u << significandBits;
                uint binarySignificand = bits & (implicitBit - 1);
                int exponentOffset = exponentBias + significandBits;

                Debug.Assert(binaryExponent != exponentMask);

                bool regular;
                if (binaryExponent == 0)
                {
                    binaryExponent = 1;
                    regular = true;
                }
                else
                {
                    regular = binarySignificand != 0;
                    binarySignificand |= implicitBit;

                    // Integers below 2^(significandBits + 1) are their own shortest representation; the caller strips trailing zeros.
                    int shift = exponentOffset - binaryExponent;
                    if (((uint)shift <= (uint)significandBits) && ((binarySignificand & ((1U << shift) - 1)) == 0))
                    {
                        significand = binarySignificand >> shift;
                        exponent = 0;
                        return;
                    }
                }

                ulong integral = ToDecimal32(binarySignificand, binaryExponent, exponentOffset, regular, out int decimalExponent, out int lastDigit);
                if (lastDigit >= 0)
                {
                    significand = integral * 10 + (uint)lastDigit;
                    exponent = decimalExponent;
                }
                else
                {
                    significand = integral;
                    exponent = decimalExponent + 1;
                }
            }

            // Returns the integral part of the scaled value; lastDigit is -1 when the integral part alone is
            // the shortest representation, otherwise the extra digit to append.
            private static ulong ToDecimal(ulong binarySignificand, int rawExponent, bool regular, out int decimalExponent, out int lastDigit)
            {
                int binaryExponent = rawExponent - DoubleExponentOffset;

                if (!regular)
                {
                    int decExp = ComputeDecimalExponent(binaryExponent, regular: false);
                    int shift = ComputeExponentShift(binaryExponent, decExp + 1) + ExtraShift;
                    GetPowerOf10(-decExp - 1, out ulong pow10High, out ulong pow10Low);
                    ulong y = binarySignificand << shift;
                    ulong productHigh = Math.BigMul(pow10High, y, out ulong productLow);
                    ulong lowProductHigh = Math.BigMul(pow10Low, y, out _);
                    ulong sumLow = productLow + lowProductHigh;
                    ulong sumHigh = productHigh + ((sumLow < productLow) ? 1UL : 0UL);

                    ulong integral = sumHigh >> ExtraShift;
                    ulong fractional = (sumHigh << (64 - ExtraShift)) | (sumLow >> ExtraShift);
                    ulong halfUlp = pow10High >> (ExtraShift + 1 - shift);
                    // The lower half-ulp gets the same one-unit allowance the regular path gives even significands
                    // (a power of two is always even): a candidate exactly on the lower boundary round-trips under
                    // ties-to-even. Only the coarse 16-bit formats reach such candidates (Half 8192 -> "8190",
                    // 16384 -> "16380"); no float or double does.
                    ulong halfUlpLow = (halfUlp >> 1) + 1;
                    bool roundUp = halfUlp > ulong.MaxValue - fractional;
                    bool roundDown = halfUlpLow > fractional;
                    // When both shorter candidates fit, take the nearer one (ties to the even candidate).
                    bool preferUp = roundUp && (!roundDown || (fractional > (1UL << 63)) || ((fractional == (1UL << 63)) && ((integral & 1) != 0)));
                    integral += preferUp ? 1UL : 0UL;

                    int digit = (int)Multiply128AddHigh64(fractional, 10, 0x7FFF_FFFF_FFFF_FFFFUL);
                    int lowDigit = (int)Multiply128AddHigh64(fractional - halfUlpLow, 10, ulong.MaxValue);
                    if (digit < lowDigit)
                    {
                        digit = lowDigit;
                    }

                    decimalExponent = decExp;
                    lastDigit = (!roundUp && !roundDown) ? digit : -1;
                    return integral;
                }

                int decimalExp = ComputeDecimalExponent(binaryExponent, regular: true);
                int regularShift = ComputeExponentShift(binaryExponent, decimalExp + 1) + ExtraShift;
                ulong even = 1UL - (binarySignificand & 1UL);
                GetPowerOf10(-decimalExp - 1, out ulong powHigh, out ulong powLow);
                ulong scaled = binarySignificand << regularShift;
                ulong pHigh = Math.BigMul(powHigh, scaled, out ulong pLow);
                ulong lowHigh = Math.BigMul(powLow, scaled, out _);
                pLow += lowHigh;
                pHigh += (pLow < lowHigh) ? 1UL : 0UL;

                ulong integralPart = pHigh >> ExtraShift;
                ulong fractionalPart = (pHigh << (64 - ExtraShift)) | (pLow >> ExtraShift);
                ulong halfUlpRegular = (powHigh >> (ExtraShift + 1 - regularShift)) + even;
                bool roundUpRegular = fractionalPart + halfUlpRegular < fractionalPart;
                bool roundDownRegular = halfUlpRegular > fractionalPart;
                integralPart += roundUpRegular ? 1UL : 0UL;

                int extraDigit = (int)Multiply128AddHigh64(fractionalPart, 10, BiasedHalf);
                if (fractionalPart == (1UL << 62))
                {
                    extraDigit = 2;
                }

                decimalExponent = decimalExp;
                lastDigit = (!roundUpRegular && !roundDownRegular) ? extraDigit : -1;
                return integralPart;
            }

            private static ulong ToDecimal32(uint binarySignificand, int rawExponent, int exponentOffset, bool regular, out int decimalExponent, out int lastDigit)
            {
                int binaryExponent = rawExponent - exponentOffset;

                if (!regular)
                {
                    int decExp = ComputeDecimalExponent(binaryExponent, regular: false);
                    int shift = ComputeExponentShift(binaryExponent, decExp + 1) + ExtraShift;
                    GetPowerOf10(-decExp - 1, out ulong pow10High, out ulong pow10Low);
                    ulong y = (ulong)binarySignificand << shift;
                    ulong productHigh = Math.BigMul(pow10High, y, out ulong productLow);
                    ulong lowProductHigh = Math.BigMul(pow10Low, y, out _);
                    ulong sumLow = productLow + lowProductHigh;
                    ulong sumHigh = productHigh + ((sumLow < productLow) ? 1UL : 0UL);

                    ulong integral = sumHigh >> ExtraShift;
                    ulong fractional = (sumHigh << (64 - ExtraShift)) | (sumLow >> ExtraShift);
                    ulong halfUlp = pow10High >> (ExtraShift + 1 - shift);
                    // The lower half-ulp gets the same one-unit allowance the regular path gives even significands
                    // (a power of two is always even): a candidate exactly on the lower boundary round-trips under
                    // ties-to-even. Only the coarse 16-bit formats reach such candidates (Half 8192 -> "8190",
                    // 16384 -> "16380"); no float or double does.
                    ulong halfUlpLow = (halfUlp >> 1) + 1;
                    bool roundUp = halfUlp > ulong.MaxValue - fractional;
                    bool roundDown = halfUlpLow > fractional;
                    // When both shorter candidates fit, take the nearer one (ties to the even candidate).
                    bool preferUp = roundUp && (!roundDown || (fractional > (1UL << 63)) || ((fractional == (1UL << 63)) && ((integral & 1) != 0)));
                    integral += preferUp ? 1UL : 0UL;

                    int digit = (int)Multiply128AddHigh64(fractional, 10, 0x7FFF_FFFF_FFFF_FFFFUL);
                    int lowDigit = (int)Multiply128AddHigh64(fractional - halfUlpLow, 10, ulong.MaxValue);
                    if (digit < lowDigit)
                    {
                        digit = lowDigit;
                    }

                    decimalExponent = decExp;
                    lastDigit = (!roundUp && !roundDown) ? digit : -1;
                    return integral;
                }

                const int SingleExtraShift = 34;

                int decimalExp = ComputeDecimalExponent(binaryExponent, regular: true);
                int regularShift = ComputeExponentShift(binaryExponent, decimalExp + 1) + SingleExtraShift;
                ulong even = 1UL - (binarySignificand & 1U);
                GetPowerOf10(-decimalExp - 1, out ulong pow10HighRegular, out _);
                ulong product = Math.BigMul(pow10HighRegular + 1, (ulong)binarySignificand << regularShift, out _);

                ulong integralPart = product >> SingleExtraShift;
                ulong fractionalPart = product & ((1UL << SingleExtraShift) - 1);
                ulong halfUlpRegular = (pow10HighRegular >> (65 - regularShift)) + even;
                bool roundUpRegular = ((fractionalPart + halfUlpRegular) >> SingleExtraShift) != 0;
                bool roundDownRegular = halfUlpRegular > fractionalPart;
                integralPart += roundUpRegular ? 1UL : 0UL;

                int extraDigit = (int)((fractionalPart * 10 + (1UL << (SingleExtraShift - 1))) >> SingleExtraShift);
                if (fractionalPart == (1UL << (SingleExtraShift - 2)))
                {
                    extraDigit = 2;
                }

                decimalExponent = decimalExp;
                lastDigit = (!roundUpRegular && !roundDownRegular) ? extraDigit : -1;
                return integralPart;
            }

            // floor(binaryExponent * log10(2)) for regular values, floor((binaryExponent - log2(4/3)... ) * log10(2)) otherwise.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int ComputeDecimalExponent(int binaryExponent, bool regular)
            {
                const int Log10ThreeOverFourSignificand = 131_072;
                const int Log10TwoSignificand = 315_653;
                const int Log10TwoExponent = 20;
                return (binaryExponent * Log10TwoSignificand - (regular ? 0 : Log10ThreeOverFourSignificand)) >> Log10TwoExponent;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int ComputeExponentShift(int binaryExponent, int decimalExponent)
            {
                const int Log2Pow10Significand = 217_707;
                const int Log2Pow10Exponent = 16;
                int pow10BinaryExponent = (-decimalExponent * Log2Pow10Significand) >> Log2Pow10Exponent;
                return binaryExponent + pow10BinaryExponent + 1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong Multiply128AddHigh64(ulong x, ulong y, ulong c)
            {
                ulong high = Math.BigMul(x, y, out ulong low);
                low += c;
                return high + ((low < c) ? 1UL : 0UL);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static T Read<T>(ReadOnlySpan<T> table, int index) where T : struct
            {
                Debug.Assert((uint)index < (uint)table.Length);
                return Unsafe.Add(ref MemoryMarshal.GetReference(table), (nint)index);
            }

            // The normalized 128-bit significand of 10^decimalExponent, read directly from the full table.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void GetPowerOf10(int decimalExponent, out ulong high, out ulong low)
            {
                Debug.Assert((decimalExponent >= MinCacheExponent) && (decimalExponent <= MaxCacheExponent));
                int index = 2 * (decimalExponent - MinCacheExponent);
                high = Read(FullCache, index);
                low = Read(FullCache, index + 1);
            }

            // 128-bit significands of 10^q for q = -307..341, rounded down, high word then low word (vitaut/zmij's table).
            private static ReadOnlySpan<ulong> FullCache =>
            [
                0x8FD0C16206306BAB, 0xA5D3B6D479F8E056, // q = -307
                0xB3C4F1BA87BC8696, 0x8F48A4899877186C, // q = -306
                0xE0B62E2929ABA83C, 0x331ACDABFE94DE87, // q = -305
                0x8C71DCD9BA0B4925, 0x9FF0C08B7F1D0B14, // q = -304
                0xAF8E5410288E1B6F, 0x07ECF0AE5EE44DD9, // q = -303
                0xDB71E91432B1A24A, 0xC9E82CD9F69D6150, // q = -302
                0x892731AC9FAF056E, 0xBE311C083A225CD2, // q = -301
                0xAB70FE17C79AC6CA, 0x6DBD630A48AAF406, // q = -300
                0xD64D3D9DB981787D, 0x092CBBCCDAD5B108, // q = -299
                0x85F0468293F0EB4E, 0x25BBF56008C58EA5, // q = -298
                0xA76C582338ED2621, 0xAF2AF2B80AF6F24E, // q = -297
                0xD1476E2C07286FAA, 0x1AF5AF660DB4AEE1, // q = -296
                0x82CCA4DB847945CA, 0x50D98D9FC890ED4D, // q = -295
                0xA37FCE126597973C, 0xE50FF107BAB528A0, // q = -294
                0xCC5FC196FEFD7D0C, 0x1E53ED49A96272C8, // q = -293
                0xFF77B1FCBEBCDC4F, 0x25E8E89C13BB0F7A, // q = -292
                0x9FAACF3DF73609B1, 0x77B191618C54E9AC, // q = -291
                0xC795830D75038C1D, 0xD59DF5B9EF6A2417, // q = -290
                0xF97AE3D0D2446F25, 0x4B0573286B44AD1D, // q = -289
                0x9BECCE62836AC577, 0x4EE367F9430AEC32, // q = -288
                0xC2E801FB244576D5, 0x229C41F793CDA73F, // q = -287
                0xF3A20279ED56D48A, 0x6B43527578C1110F, // q = -286
                0x9845418C345644D6, 0x830A13896B78AAA9, // q = -285
                0xBE5691EF416BD60C, 0x23CC986BC656D553, // q = -284
                0xEDEC366B11C6CB8F, 0x2CBFBE86B7EC8AA8, // q = -283
                0x94B3A202EB1C3F39, 0x7BF7D71432F3D6A9, // q = -282
                0xB9E08A83A5E34F07, 0xDAF5CCD93FB0CC53, // q = -281
                0xE858AD248F5C22C9, 0xD1B3400F8F9CFF68, // q = -280
                0x91376C36D99995BE, 0x23100809B9C21FA1, // q = -279
                0xB58547448FFFFB2D, 0xABD40A0C2832A78A, // q = -278
                0xE2E69915B3FFF9F9, 0x16C90C8F323F516C, // q = -277
                0x8DD01FAD907FFC3B, 0xAE3DA7D97F6792E3, // q = -276
                0xB1442798F49FFB4A, 0x99CD11CFDF41779C, // q = -275
                0xDD95317F31C7FA1D, 0x40405643D711D583, // q = -274
                0x8A7D3EEF7F1CFC52, 0x482835EA666B2572, // q = -273
                0xAD1C8EAB5EE43B66, 0xDA3243650005EECF, // q = -272
                0xD863B256369D4A40, 0x90BED43E40076A82, // q = -271
                0x873E4F75E2224E68, 0x5A7744A6E804A291, // q = -270
                0xA90DE3535AAAE202, 0x711515D0A205CB36, // q = -269
                0xD3515C2831559A83, 0x0D5A5B44CA873E03, // q = -268
                0x8412D9991ED58091, 0xE858790AFE9486C2, // q = -267
                0xA5178FFF668AE0B6, 0x626E974DBE39A872, // q = -266
                0xCE5D73FF402D98E3, 0xFB0A3D212DC8128F, // q = -265
                0x80FA687F881C7F8E, 0x7CE66634BC9D0B99, // q = -264
                0xA139029F6A239F72, 0x1C1FFFC1EBC44E80, // q = -263
                0xC987434744AC874E, 0xA327FFB266B56220, // q = -262
                0xFBE9141915D7A922, 0x4BF1FF9F0062BAA8, // q = -261
                0x9D71AC8FADA6C9B5, 0x6F773FC3603DB4A9, // q = -260
                0xC4CE17B399107C22, 0xCB550FB4384D21D3, // q = -259
                0xF6019DA07F549B2B, 0x7E2A53A146606A48, // q = -258
                0x99C102844F94E0FB, 0x2EDA7444CBFC426D, // q = -257
                0xC0314325637A1939, 0xFA911155FEFB5308, // q = -256
                0xF03D93EEBC589F88, 0x793555AB7EBA27CA, // q = -255
                0x96267C7535B763B5, 0x4BC1558B2F3458DE, // q = -254
                0xBBB01B9283253CA2, 0x9EB1AAEDFB016F16, // q = -253
                0xEA9C227723EE8BCB, 0x465E15A979C1CADC, // q = -252
                0x92A1958A7675175F, 0x0BFACD89EC191EC9, // q = -251
                0xB749FAED14125D36, 0xCEF980EC671F667B, // q = -250
                0xE51C79A85916F484, 0x82B7E12780E7401A, // q = -249
                0x8F31CC0937AE58D2, 0xD1B2ECB8B0908810, // q = -248
                0xB2FE3F0B8599EF07, 0x861FA7E6DCB4AA15, // q = -247
                0xDFBDCECE67006AC9, 0x67A791E093E1D49A, // q = -246
                0x8BD6A141006042BD, 0xE0C8BB2C5C6D24E0, // q = -245
                0xAECC49914078536D, 0x58FAE9F773886E18, // q = -244
                0xDA7F5BF590966848, 0xAF39A475506A899E, // q = -243
                0x888F99797A5E012D, 0x6D8406C952429603, // q = -242
                0xAAB37FD7D8F58178, 0xC8E5087BA6D33B83, // q = -241
                0xD5605FCDCF32E1D6, 0xFB1E4A9A90880A64, // q = -240
                0x855C3BE0A17FCD26, 0x5CF2EEA09A55067F, // q = -239
                0xA6B34AD8C9DFC06F, 0xF42FAA48C0EA481E, // q = -238
                0xD0601D8EFC57B08B, 0xF13B94DAF124DA26, // q = -237
                0x823C12795DB6CE57, 0x76C53D08D6B70858, // q = -236
                0xA2CB1717B52481ED, 0x54768C4B0C64CA6E, // q = -235
                0xCB7DDCDDA26DA268, 0xA9942F5DCF7DFD09, // q = -234
                0xFE5D54150B090B02, 0xD3F93B35435D7C4C, // q = -233
                0x9EFA548D26E5A6E1, 0xC47BC5014A1A6DAF, // q = -232
                0xC6B8E9B0709F109A, 0x359AB6419CA1091B, // q = -231
                0xF867241C8CC6D4C0, 0xC30163D203C94B62, // q = -230
                0x9B407691D7FC44F8, 0x79E0DE63425DCF1D, // q = -229
                0xC21094364DFB5636, 0x985915FC12F542E4, // q = -228
                0xF294B943E17A2BC4, 0x3E6F5B7B17B2939D, // q = -227
                0x979CF3CA6CEC5B5A, 0xA705992CEECF9C42, // q = -226
                0xBD8430BD08277231, 0x50C6FF782A838353, // q = -225
                0xECE53CEC4A314EBD, 0xA4F8BF5635246428, // q = -224
                0x940F4613AE5ED136, 0x871B7795E136BE99, // q = -223
                0xB913179899F68584, 0x28E2557B59846E3F, // q = -222
                0xE757DD7EC07426E5, 0x331AEADA2FE589CF, // q = -221
                0x9096EA6F3848984F, 0x3FF0D2C85DEF7621, // q = -220
                0xB4BCA50B065ABE63, 0x0FED077A756B53A9, // q = -219
                0xE1EBCE4DC7F16DFB, 0xD3E8495912C62894, // q = -218
                0x8D3360F09CF6E4BD, 0x64712DD7ABBBD95C, // q = -217
                0xB080392CC4349DEC, 0xBD8D794D96AACFB3, // q = -216
                0xDCA04777F541C567, 0xECF0D7A0FC5583A0, // q = -215
                0x89E42CAAF9491B60, 0xF41686C49DB57244, // q = -214
                0xAC5D37D5B79B6239, 0x311C2875C522CED5, // q = -213
                0xD77485CB25823AC7, 0x7D633293366B828B, // q = -212
                0x86A8D39EF77164BC, 0xAE5DFF9C02033197, // q = -211
                0xA8530886B54DBDEB, 0xD9F57F830283FDFC, // q = -210
                0xD267CAA862A12D66, 0xD072DF63C324FD7B, // q = -209
                0x8380DEA93DA4BC60, 0x4247CB9E59F71E6D, // q = -208
                0xA46116538D0DEB78, 0x52D9BE85F074E608, // q = -207
                0xCD795BE870516656, 0x67902E276C921F8B, // q = -206
                0x806BD9714632DFF6, 0x00BA1CD8A3DB53B6, // q = -205
                0xA086CFCD97BF97F3, 0x80E8A40ECCD228A4, // q = -204
                0xC8A883C0FDAF7DF0, 0x6122CD128006B2CD, // q = -203
                0xFAD2A4B13D1B5D6C, 0x796B805720085F81, // q = -202
                0x9CC3A6EEC6311A63, 0xCBE3303674053BB0, // q = -201
                0xC3F490AA77BD60FC, 0xBEDBFC4411068A9C, // q = -200
                0xF4F1B4D515ACB93B, 0xEE92FB5515482D44, // q = -199
                0x991711052D8BF3C5, 0x751BDD152D4D1C4A, // q = -198
                0xBF5CD54678EEF0B6, 0xD262D45A78A0635D, // q = -197
                0xEF340A98172AACE4, 0x86FB897116C87C34, // q = -196
                0x9580869F0E7AAC0E, 0xD45D35E6AE3D4DA0, // q = -195
                0xBAE0A846D2195712, 0x8974836059CCA109, // q = -194
                0xE998D258869FACD7, 0x2BD1A438703FC94B, // q = -193
                0x91FF83775423CC06, 0x7B6306A34627DDCF, // q = -192
                0xB67F6455292CBF08, 0x1A3BC84C17B1D542, // q = -191
                0xE41F3D6A7377EECA, 0x20CABA5F1D9E4A93, // q = -190
                0x8E938662882AF53E, 0x547EB47B7282EE9C, // q = -189
                0xB23867FB2A35B28D, 0xE99E619A4F23AA43, // q = -188
                0xDEC681F9F4C31F31, 0x6405FA00E2EC94D4, // q = -187
                0x8B3C113C38F9F37E, 0xDE83BC408DD3DD04, // q = -186
                0xAE0B158B4738705E, 0x9624AB50B148D445, // q = -185
                0xD98DDAEE19068C76, 0x3BADD624DD9B0957, // q = -184
                0x87F8A8D4CFA417C9, 0xE54CA5D70A80E5D6, // q = -183
                0xA9F6D30A038D1DBC, 0x5E9FCF4CCD211F4C, // q = -182
                0xD47487CC8470652B, 0x7647C3200069671F, // q = -181
                0x84C8D4DFD2C63F3B, 0x29ECD9F40041E073, // q = -180
                0xA5FB0A17C777CF09, 0xF468107100525890, // q = -179
                0xCF79CC9DB955C2CC, 0x7182148D4066EEB4, // q = -178
                0x81AC1FE293D599BF, 0xC6F14CD848405530, // q = -177
                0xA21727DB38CB002F, 0xB8ADA00E5A506A7C, // q = -176
                0xCA9CF1D206FDC03B, 0xA6D90811F0E4851C, // q = -175
                0xFD442E4688BD304A, 0x908F4A166D1DA663, // q = -174
                0x9E4A9CEC15763E2E, 0x9A598E4E043287FE, // q = -173
                0xC5DD44271AD3CDBA, 0x40EFF1E1853F29FD, // q = -172
                0xF7549530E188C128, 0xD12BEE59E68EF47C, // q = -171
                0x9A94DD3E8CF578B9, 0x82BB74F8301958CE, // q = -170
                0xC13A148E3032D6E7, 0xE36A52363C1FAF01, // q = -169
                0xF18899B1BC3F8CA1, 0xDC44E6C3CB279AC1, // q = -168
                0x96F5600F15A7B7E5, 0x29AB103A5EF8C0B9, // q = -167
                0xBCB2B812DB11A5DE, 0x7415D448F6B6F0E7, // q = -166
                0xEBDF661791D60F56, 0x111B495B3464AD21, // q = -165
                0x936B9FCEBB25C995, 0xCAB10DD900BEEC34, // q = -164
                0xB84687C269EF3BFB, 0x3D5D514F40EEA742, // q = -163
                0xE65829B3046B0AFA, 0x0CB4A5A3112A5112, // q = -162
                0x8FF71A0FE2C2E6DC, 0x47F0E785EABA72AB, // q = -161
                0xB3F4E093DB73A093, 0x59ED216765690F56, // q = -160
                0xE0F218B8D25088B8, 0x306869C13EC3532C, // q = -159
                0x8C974F7383725573, 0x1E414218C73A13FB, // q = -158
                0xAFBD2350644EEACF, 0xE5D1929EF90898FA, // q = -157
                0xDBAC6C247D62A583, 0xDF45F746B74ABF39, // q = -156
                0x894BC396CE5DA772, 0x6B8BBA8C328EB783, // q = -155
                0xAB9EB47C81F5114F, 0x066EA92F3F326564, // q = -154
                0xD686619BA27255A2, 0xC80A537B0EFEFEBD, // q = -153
                0x8613FD0145877585, 0xBD06742CE95F5F36, // q = -152
                0xA798FC4196E952E7, 0x2C48113823B73704, // q = -151
                0xD17F3B51FCA3A7A0, 0xF75A15862CA504C5, // q = -150
                0x82EF85133DE648C4, 0x9A984D73DBE722FB, // q = -149
                0xA3AB66580D5FDAF5, 0xC13E60D0D2E0EBBA, // q = -148
                0xCC963FEE10B7D1B3, 0x318DF905079926A8, // q = -147
                0xFFBBCFE994E5C61F, 0xFDF17746497F7052, // q = -146
                0x9FD561F1FD0F9BD3, 0xFEB6EA8BEDEFA633, // q = -145
                0xC7CABA6E7C5382C8, 0xFE64A52EE96B8FC0, // q = -144
                0xF9BD690A1B68637B, 0x3DFDCE7AA3C673B0, // q = -143
                0x9C1661A651213E2D, 0x06BEA10CA65C084E, // q = -142
                0xC31BFA0FE5698DB8, 0x486E494FCFF30A62, // q = -141
                0xF3E2F893DEC3F126, 0x5A89DBA3C3EFCCFA, // q = -140
                0x986DDB5C6B3A76B7, 0xF89629465A75E01C, // q = -139
                0xBE89523386091465, 0xF6BBB397F1135823, // q = -138
                0xEE2BA6C0678B597F, 0x746AA07DED582E2C, // q = -137
                0x94DB483840B717EF, 0xA8C2A44EB4571CDC, // q = -136
                0xBA121A4650E4DDEB, 0x92F34D62616CE413, // q = -135
                0xE896A0D7E51E1566, 0x77B020BAF9C81D17, // q = -134
                0x915E2486EF32CD60, 0x0ACE1474DC1D122E, // q = -133
                0xB5B5ADA8AAFF80B8, 0x0D819992132456BA, // q = -132
                0xE3231912D5BF60E6, 0x10E1FFF697ED6C69, // q = -131
                0x8DF5EFABC5979C8F, 0xCA8D3FFA1EF463C1, // q = -130
                0xB1736B96B6FD83B3, 0xBD308FF8A6B17CB2, // q = -129
                0xDDD0467C64BCE4A0, 0xAC7CB3F6D05DDBDE, // q = -128
                0x8AA22C0DBEF60EE4, 0x6BCDF07A423AA96B, // q = -127
                0xAD4AB7112EB3929D, 0x86C16C98D2C953C6, // q = -126
                0xD89D64D57A607744, 0xE871C7BF077BA8B7, // q = -125
                0x87625F056C7C4A8B, 0x11471CD764AD4972, // q = -124
                0xA93AF6C6C79B5D2D, 0xD598E40D3DD89BCF, // q = -123
                0xD389B47879823479, 0x4AFF1D108D4EC2C3, // q = -122
                0x843610CB4BF160CB, 0xCEDF722A585139BA, // q = -121
                0xA54394FE1EEDB8FE, 0xC2974EB4EE658828, // q = -120
                0xCE947A3DA6A9273E, 0x733D226229FEEA32, // q = -119
                0x811CCC668829B887, 0x0806357D5A3F525F, // q = -118
                0xA163FF802A3426A8, 0xCA07C2DCB0CF26F7, // q = -117
                0xC9BCFF6034C13052, 0xFC89B393DD02F0B5, // q = -116
                0xFC2C3F3841F17C67, 0xBBAC2078D443ACE2, // q = -115
                0x9D9BA7832936EDC0, 0xD54B944B84AA4C0D, // q = -114
                0xC5029163F384A931, 0x0A9E795E65D4DF11, // q = -113
                0xF64335BCF065D37D, 0x4D4617B5FF4A16D5, // q = -112
                0x99EA0196163FA42E, 0x504BCED1BF8E4E45, // q = -111
                0xC06481FB9BCF8D39, 0xE45EC2862F71E1D6, // q = -110
                0xF07DA27A82C37088, 0x5D767327BB4E5A4C, // q = -109
                0x964E858C91BA2655, 0x3A6A07F8D510F86F, // q = -108
                0xBBE226EFB628AFEA, 0x890489F70A55368B, // q = -107
                0xEADAB0ABA3B2DBE5, 0x2B45AC74CCEA842E, // q = -106
                0x92C8AE6B464FC96F, 0x3B0B8BC90012929D, // q = -105
                0xB77ADA0617E3BBCB, 0x09CE6EBB40173744, // q = -104
                0xE55990879DDCAABD, 0xCC420A6A101D0515, // q = -103
                0x8F57FA54C2A9EAB6, 0x9FA946824A12232D, // q = -102
                0xB32DF8E9F3546564, 0x47939822DC96ABF9, // q = -101
                0xDFF9772470297EBD, 0x59787E2B93BC56F7, // q = -100
                0x8BFBEA76C619EF36, 0x57EB4EDB3C55B65A, // q = -99
                0xAEFAE51477A06B03, 0xEDE622920B6B23F1, // q = -98
                0xDAB99E59958885C4, 0xE95FAB368E45ECED, // q = -97
                0x88B402F7FD75539B, 0x11DBCB0218EBB414, // q = -96
                0xAAE103B5FCD2A881, 0xD652BDC29F26A119, // q = -95
                0xD59944A37C0752A2, 0x4BE76D3346F0495F, // q = -94
                0x857FCAE62D8493A5, 0x6F70A4400C562DDB, // q = -93
                0xA6DFBD9FB8E5B88E, 0xCB4CCD500F6BB952, // q = -92
                0xD097AD07A71F26B2, 0x7E2000A41346A7A7, // q = -91
                0x825ECC24C873782F, 0x8ED400668C0C28C8, // q = -90
                0xA2F67F2DFA90563B, 0x728900802F0F32FA, // q = -89
                0xCBB41EF979346BCA, 0x4F2B40A03AD2FFB9, // q = -88
                0xFEA126B7D78186BC, 0xE2F610C84987BFA8, // q = -87
                0x9F24B832E6B0F436, 0x0DD9CA7D2DF4D7C9, // q = -86
                0xC6EDE63FA05D3143, 0x91503D1C79720DBB, // q = -85
                0xF8A95FCF88747D94, 0x75A44C6397CE912A, // q = -84
                0x9B69DBE1B548CE7C, 0xC986AFBE3EE11ABA, // q = -83
                0xC24452DA229B021B, 0xFBE85BADCE996168, // q = -82
                0xF2D56790AB41C2A2, 0xFAE27299423FB9C3, // q = -81
                0x97C560BA6B0919A5, 0xDCCD879FC967D41A, // q = -80
                0xBDB6B8E905CB600F, 0x5400E987BBC1C920, // q = -79
                0xED246723473E3813, 0x290123E9AAB23B68, // q = -78
                0x9436C0760C86E30B, 0xF9A0B6720AAF6521, // q = -77
                0xB94470938FA89BCE, 0xF808E40E8D5B3E69, // q = -76
                0xE7958CB87392C2C2, 0xB60B1D1230B20E04, // q = -75
                0x90BD77F3483BB9B9, 0xB1C6F22B5E6F48C2, // q = -74
                0xB4ECD5F01A4AA828, 0x1E38AEB6360B1AF3, // q = -73
                0xE2280B6C20DD5232, 0x25C6DA63C38DE1B0, // q = -72
                0x8D590723948A535F, 0x579C487E5A38AD0E, // q = -71
                0xB0AF48EC79ACE837, 0x2D835A9DF0C6D851, // q = -70
                0xDCDB1B2798182244, 0xF8E431456CF88E65, // q = -69
                0x8A08F0F8BF0F156B, 0x1B8E9ECB641B58FF, // q = -68
                0xAC8B2D36EED2DAC5, 0xE272467E3D222F3F, // q = -67
                0xD7ADF884AA879177, 0x5B0ED81DCC6ABB0F, // q = -66
                0x86CCBB52EA94BAEA, 0x98E947129FC2B4E9, // q = -65
                0xA87FEA27A539E9A5, 0x3F2398D747B36224, // q = -64
                0xD29FE4B18E88640E, 0x8EEC7F0D19A03AAD, // q = -63
                0x83A3EEEEF9153E89, 0x1953CF68300424AC, // q = -62
                0xA48CEAAAB75A8E2B, 0x5FA8C3423C052DD7, // q = -61
                0xCDB02555653131B6, 0x3792F412CB06794D, // q = -60
                0x808E17555F3EBF11, 0xE2BBD88BBEE40BD0, // q = -59
                0xA0B19D2AB70E6ED6, 0x5B6ACEAEAE9D0EC4, // q = -58
                0xC8DE047564D20A8B, 0xF245825A5A445275, // q = -57
                0xFB158592BE068D2E, 0xEED6E2F0F0D56712, // q = -56
                0x9CED737BB6C4183D, 0x55464DD69685606B, // q = -55
                0xC428D05AA4751E4C, 0xAA97E14C3C26B886, // q = -54
                0xF53304714D9265DF, 0xD53DD99F4B3066A8, // q = -53
                0x993FE2C6D07B7FAB, 0xE546A8038EFE4029, // q = -52
                0xBF8FDB78849A5F96, 0xDE98520472BDD033, // q = -51
                0xEF73D256A5C0F77C, 0x963E66858F6D4440, // q = -50
                0x95A8637627989AAD, 0xDDE7001379A44AA8, // q = -49
                0xBB127C53B17EC159, 0x5560C018580D5D52, // q = -48
                0xE9D71B689DDE71AF, 0xAAB8F01E6E10B4A6, // q = -47
                0x9226712162AB070D, 0xCAB3961304CA70E8, // q = -46
                0xB6B00D69BB55C8D1, 0x3D607B97C5FD0D22, // q = -45
                0xE45C10C42A2B3B05, 0x8CB89A7DB77C506A, // q = -44
                0x8EB98A7A9A5B04E3, 0x77F3608E92ADB242, // q = -43
                0xB267ED1940F1C61C, 0x55F038B237591ED3, // q = -42
                0xDF01E85F912E37A3, 0x6B6C46DEC52F6688, // q = -41
                0x8B61313BBABCE2C6, 0x2323AC4B3B3DA015, // q = -40
                0xAE397D8AA96C1B77, 0xABEC975E0A0D081A, // q = -39
                0xD9C7DCED53C72255, 0x96E7BD358C904A21, // q = -38
                0x881CEA14545C7575, 0x7E50D64177DA2E54, // q = -37
                0xAA242499697392D2, 0xDDE50BD1D5D0B9E9, // q = -36
                0xD4AD2DBFC3D07787, 0x955E4EC64B44E864, // q = -35
                0x84EC3C97DA624AB4, 0xBD5AF13BEF0B113E, // q = -34
                0xA6274BBDD0FADD61, 0xECB1AD8AEACDD58E, // q = -33
                0xCFB11EAD453994BA, 0x67DE18EDA5814AF2, // q = -32
                0x81CEB32C4B43FCF4, 0x80EACF948770CED7, // q = -31
                0xA2425FF75E14FC31, 0xA1258379A94D028D, // q = -30
                0xCAD2F7F5359A3B3E, 0x096EE45813A04330, // q = -29
                0xFD87B5F28300CA0D, 0x8BCA9D6E188853FC, // q = -28
                0x9E74D1B791E07E48, 0x775EA264CF55347D, // q = -27
                0xC612062576589DDA, 0x95364AFE032A819D, // q = -26
                0xF79687AED3EEC551, 0x3A83DDBD83F52204, // q = -25
                0x9ABE14CD44753B52, 0xC4926A9672793542, // q = -24
                0xC16D9A0095928A27, 0x75B7053C0F178293, // q = -23
                0xF1C90080BAF72CB1, 0x5324C68B12DD6338, // q = -22
                0x971DA05074DA7BEE, 0xD3F6FC16EBCA5E03, // q = -21
                0xBCE5086492111AEA, 0x88F4BB1CA6BCF584, // q = -20
                0xEC1E4A7DB69561A5, 0x2B31E9E3D06C32E5, // q = -19
                0x9392EE8E921D5D07, 0x3AFF322E62439FCF, // q = -18
                0xB877AA3236A4B449, 0x09BEFEB9FAD487C2, // q = -17
                0xE69594BEC44DE15B, 0x4C2EBE687989A9B3, // q = -16
                0x901D7CF73AB0ACD9, 0x0F9D37014BF60A10, // q = -15
                0xB424DC35095CD80F, 0x538484C19EF38C94, // q = -14
                0xE12E13424BB40E13, 0x2865A5F206B06FB9, // q = -13
                0x8CBCCC096F5088CB, 0xF93F87B7442E45D3, // q = -12
                0xAFEBFF0BCB24AAFE, 0xF78F69A51539D748, // q = -11
                0xDBE6FECEBDEDD5BE, 0xB573440E5A884D1B, // q = -10
                0x89705F4136B4A597, 0x31680A88F8953030, // q = -9
                0xABCC77118461CEFC, 0xFDC20D2B36BA7C3D, // q = -8
                0xD6BF94D5E57A42BC, 0x3D32907604691B4C, // q = -7
                0x8637BD05AF6C69B5, 0xA63F9A49C2C1B10F, // q = -6
                0xA7C5AC471B478423, 0x0FCF80DC33721D53, // q = -5
                0xD1B71758E219652B, 0xD3C36113404EA4A8, // q = -4
                0x83126E978D4FDF3B, 0x645A1CAC083126E9, // q = -3
                0xA3D70A3D70A3D70A, 0x3D70A3D70A3D70A3, // q = -2
                0xCCCCCCCCCCCCCCCC, 0xCCCCCCCCCCCCCCCC, // q = -1
                0x8000000000000000, 0x0000000000000000, // q = 0
                0xA000000000000000, 0x0000000000000000, // q = 1
                0xC800000000000000, 0x0000000000000000, // q = 2
                0xFA00000000000000, 0x0000000000000000, // q = 3
                0x9C40000000000000, 0x0000000000000000, // q = 4
                0xC350000000000000, 0x0000000000000000, // q = 5
                0xF424000000000000, 0x0000000000000000, // q = 6
                0x9896800000000000, 0x0000000000000000, // q = 7
                0xBEBC200000000000, 0x0000000000000000, // q = 8
                0xEE6B280000000000, 0x0000000000000000, // q = 9
                0x9502F90000000000, 0x0000000000000000, // q = 10
                0xBA43B74000000000, 0x0000000000000000, // q = 11
                0xE8D4A51000000000, 0x0000000000000000, // q = 12
                0x9184E72A00000000, 0x0000000000000000, // q = 13
                0xB5E620F480000000, 0x0000000000000000, // q = 14
                0xE35FA931A0000000, 0x0000000000000000, // q = 15
                0x8E1BC9BF04000000, 0x0000000000000000, // q = 16
                0xB1A2BC2EC5000000, 0x0000000000000000, // q = 17
                0xDE0B6B3A76400000, 0x0000000000000000, // q = 18
                0x8AC7230489E80000, 0x0000000000000000, // q = 19
                0xAD78EBC5AC620000, 0x0000000000000000, // q = 20
                0xD8D726B7177A8000, 0x0000000000000000, // q = 21
                0x878678326EAC9000, 0x0000000000000000, // q = 22
                0xA968163F0A57B400, 0x0000000000000000, // q = 23
                0xD3C21BCECCEDA100, 0x0000000000000000, // q = 24
                0x84595161401484A0, 0x0000000000000000, // q = 25
                0xA56FA5B99019A5C8, 0x0000000000000000, // q = 26
                0xCECB8F27F4200F3A, 0x0000000000000000, // q = 27
                0x813F3978F8940984, 0x4000000000000000, // q = 28
                0xA18F07D736B90BE5, 0x5000000000000000, // q = 29
                0xC9F2C9CD04674EDE, 0xA400000000000000, // q = 30
                0xFC6F7C4045812296, 0x4D00000000000000, // q = 31
                0x9DC5ADA82B70B59D, 0xF020000000000000, // q = 32
                0xC5371912364CE305, 0x6C28000000000000, // q = 33
                0xF684DF56C3E01BC6, 0xC732000000000000, // q = 34
                0x9A130B963A6C115C, 0x3C7F400000000000, // q = 35
                0xC097CE7BC90715B3, 0x4B9F100000000000, // q = 36
                0xF0BDC21ABB48DB20, 0x1E86D40000000000, // q = 37
                0x96769950B50D88F4, 0x1314448000000000, // q = 38
                0xBC143FA4E250EB31, 0x17D955A000000000, // q = 39
                0xEB194F8E1AE525FD, 0x5DCFAB0800000000, // q = 40
                0x92EFD1B8D0CF37BE, 0x5AA1CAE500000000, // q = 41
                0xB7ABC627050305AD, 0xF14A3D9E40000000, // q = 42
                0xE596B7B0C643C719, 0x6D9CCD05D0000000, // q = 43
                0x8F7E32CE7BEA5C6F, 0xE4820023A2000000, // q = 44
                0xB35DBF821AE4F38B, 0xDDA2802C8A800000, // q = 45
                0xE0352F62A19E306E, 0xD50B2037AD200000, // q = 46
                0x8C213D9DA502DE45, 0x4526F422CC340000, // q = 47
                0xAF298D050E4395D6, 0x9670B12B7F410000, // q = 48
                0xDAF3F04651D47B4C, 0x3C0CDD765F114000, // q = 49
                0x88D8762BF324CD0F, 0xA5880A69FB6AC800, // q = 50
                0xAB0E93B6EFEE0053, 0x8EEA0D047A457A00, // q = 51
                0xD5D238A4ABE98068, 0x72A4904598D6D880, // q = 52
                0x85A36366EB71F041, 0x47A6DA2B7F864750, // q = 53
                0xA70C3C40A64E6C51, 0x999090B65F67D924, // q = 54
                0xD0CF4B50CFE20765, 0xFFF4B4E3F741CF6D, // q = 55
                0x82818F1281ED449F, 0xBFF8F10E7A8921A4, // q = 56
                0xA321F2D7226895C7, 0xAFF72D52192B6A0D, // q = 57
                0xCBEA6F8CEB02BB39, 0x9BF4F8A69F764490, // q = 58
                0xFEE50B7025C36A08, 0x02F236D04753D5B4, // q = 59
                0x9F4F2726179A2245, 0x01D762422C946590, // q = 60
                0xC722F0EF9D80AAD6, 0x424D3AD2B7B97EF5, // q = 61
                0xF8EBAD2B84E0D58B, 0xD2E0898765A7DEB2, // q = 62
                0x9B934C3B330C8577, 0x63CC55F49F88EB2F, // q = 63
                0xC2781F49FFCFA6D5, 0x3CBF6B71C76B25FB, // q = 64
                0xF316271C7FC3908A, 0x8BEF464E3945EF7A, // q = 65
                0x97EDD871CFDA3A56, 0x97758BF0E3CBB5AC, // q = 66
                0xBDE94E8E43D0C8EC, 0x3D52EEED1CBEA317, // q = 67
                0xED63A231D4C4FB27, 0x4CA7AAA863EE4BDD, // q = 68
                0x945E455F24FB1CF8, 0x8FE8CAA93E74EF6A, // q = 69
                0xB975D6B6EE39E436, 0xB3E2FD538E122B44, // q = 70
                0xE7D34C64A9C85D44, 0x60DBBCA87196B616, // q = 71
                0x90E40FBEEA1D3A4A, 0xBC8955E946FE31CD, // q = 72
                0xB51D13AEA4A488DD, 0x6BABAB6398BDBE41, // q = 73
                0xE264589A4DCDAB14, 0xC696963C7EED2DD1, // q = 74
                0x8D7EB76070A08AEC, 0xFC1E1DE5CF543CA2, // q = 75
                0xB0DE65388CC8ADA8, 0x3B25A55F43294BCB, // q = 76
                0xDD15FE86AFFAD912, 0x49EF0EB713F39EBE, // q = 77
                0x8A2DBF142DFCC7AB, 0x6E3569326C784337, // q = 78
                0xACB92ED9397BF996, 0x49C2C37F07965404, // q = 79
                0xD7E77A8F87DAF7FB, 0xDC33745EC97BE906, // q = 80
                0x86F0AC99B4E8DAFD, 0x69A028BB3DED71A3, // q = 81
                0xA8ACD7C0222311BC, 0xC40832EA0D68CE0C, // q = 82
                0xD2D80DB02AABD62B, 0xF50A3FA490C30190, // q = 83
                0x83C7088E1AAB65DB, 0x792667C6DA79E0FA, // q = 84
                0xA4B8CAB1A1563F52, 0x577001B891185938, // q = 85
                0xCDE6FD5E09ABCF26, 0xED4C0226B55E6F86, // q = 86
                0x80B05E5AC60B6178, 0x544F8158315B05B4, // q = 87
                0xA0DC75F1778E39D6, 0x696361AE3DB1C721, // q = 88
                0xC913936DD571C84C, 0x03BC3A19CD1E38E9, // q = 89
                0xFB5878494ACE3A5F, 0x04AB48A04065C723, // q = 90
                0x9D174B2DCEC0E47B, 0x62EB0D64283F9C76, // q = 91
                0xC45D1DF942711D9A, 0x3BA5D0BD324F8394, // q = 92
                0xF5746577930D6500, 0xCA8F44EC7EE36479, // q = 93
                0x9968BF6ABBE85F20, 0x7E998B13CF4E1ECB, // q = 94
                0xBFC2EF456AE276E8, 0x9E3FEDD8C321A67E, // q = 95
                0xEFB3AB16C59B14A2, 0xC5CFE94EF3EA101E, // q = 96
                0x95D04AEE3B80ECE5, 0xBBA1F1D158724A12, // q = 97
                0xBB445DA9CA61281F, 0x2A8A6E45AE8EDC97, // q = 98
                0xEA1575143CF97226, 0xF52D09D71A3293BD, // q = 99
                0x924D692CA61BE758, 0x593C2626705F9C56, // q = 100
                0xB6E0C377CFA2E12E, 0x6F8B2FB00C77836C, // q = 101
                0xE498F455C38B997A, 0x0B6DFB9C0F956447, // q = 102
                0x8EDF98B59A373FEC, 0x4724BD4189BD5EAC, // q = 103
                0xB2977EE300C50FE7, 0x58EDEC91EC2CB657, // q = 104
                0xDF3D5E9BC0F653E1, 0x2F2967B66737E3ED, // q = 105
                0x8B865B215899F46C, 0xBD79E0D20082EE74, // q = 106
                0xAE67F1E9AEC07187, 0xECD8590680A3AA11, // q = 107
                0xDA01EE641A708DE9, 0xE80E6F4820CC9495, // q = 108
                0x884134FE908658B2, 0x3109058D147FDCDD, // q = 109
                0xAA51823E34A7EEDE, 0xBD4B46F0599FD415, // q = 110
                0xD4E5E2CDC1D1EA96, 0x6C9E18AC7007C91A, // q = 111
                0x850FADC09923329E, 0x03E2CF6BC604DDB0, // q = 112
                0xA6539930BF6BFF45, 0x84DB8346B786151C, // q = 113
                0xCFE87F7CEF46FF16, 0xE612641865679A63, // q = 114
                0x81F14FAE158C5F6E, 0x4FCB7E8F3F60C07E, // q = 115
                0xA26DA3999AEF7749, 0xE3BE5E330F38F09D, // q = 116
                0xCB090C8001AB551C, 0x5CADF5BFD3072CC5, // q = 117
                0xFDCB4FA002162A63, 0x73D9732FC7C8F7F6, // q = 118
                0x9E9F11C4014DDA7E, 0x2867E7FDDCDD9AFA, // q = 119
                0xC646D63501A1511D, 0xB281E1FD541501B8, // q = 120
                0xF7D88BC24209A565, 0x1F225A7CA91A4226, // q = 121
                0x9AE757596946075F, 0x3375788DE9B06958, // q = 122
                0xC1A12D2FC3978937, 0x0052D6B1641C83AE, // q = 123
                0xF209787BB47D6B84, 0xC0678C5DBD23A49A, // q = 124
                0x9745EB4D50CE6332, 0xF840B7BA963646E0, // q = 125
                0xBD176620A501FBFF, 0xB650E5A93BC3D898, // q = 126
                0xEC5D3FA8CE427AFF, 0xA3E51F138AB4CEBE, // q = 127
                0x93BA47C980E98CDF, 0xC66F336C36B10137, // q = 128
                0xB8A8D9BBE123F017, 0xB80B0047445D4184, // q = 129
                0xE6D3102AD96CEC1D, 0xA60DC059157491E5, // q = 130
                0x9043EA1AC7E41392, 0x87C89837AD68DB2F, // q = 131
                0xB454E4A179DD1877, 0x29BABE4598C311FB, // q = 132
                0xE16A1DC9D8545E94, 0xF4296DD6FEF3D67A, // q = 133
                0x8CE2529E2734BB1D, 0x1899E4A65F58660C, // q = 134
                0xB01AE745B101E9E4, 0x5EC05DCFF72E7F8F, // q = 135
                0xDC21A1171D42645D, 0x76707543F4FA1F73, // q = 136
                0x899504AE72497EBA, 0x6A06494A791C53A8, // q = 137
                0xABFA45DA0EDBDE69, 0x0487DB9D17636892, // q = 138
                0xD6F8D7509292D603, 0x45A9D2845D3C42B6, // q = 139
                0x865B86925B9BC5C2, 0x0B8A2392BA45A9B2, // q = 140
                0xA7F26836F282B732, 0x8E6CAC7768D7141E, // q = 141
                0xD1EF0244AF2364FF, 0x3207D795430CD926, // q = 142
                0x8335616AED761F1F, 0x7F44E6BD49E807B8, // q = 143
                0xA402B9C5A8D3A6E7, 0x5F16206C9C6209A6, // q = 144
                0xCD036837130890A1, 0x36DBA887C37A8C0F, // q = 145
                0x802221226BE55A64, 0xC2494954DA2C9789, // q = 146
                0xA02AA96B06DEB0FD, 0xF2DB9BAA10B7BD6C, // q = 147
                0xC83553C5C8965D3D, 0x6F92829494E5ACC7, // q = 148
                0xFA42A8B73ABBF48C, 0xCB772339BA1F17F9, // q = 149
                0x9C69A97284B578D7, 0xFF2A760414536EFB, // q = 150
                0xC38413CF25E2D70D, 0xFEF5138519684ABA, // q = 151
                0xF46518C2EF5B8CD1, 0x7EB258665FC25D69, // q = 152
                0x98BF2F79D5993802, 0xEF2F773FFBD97A61, // q = 153
                0xBEEEFB584AFF8603, 0xAAFB550FFACFD8FA, // q = 154
                0xEEAABA2E5DBF6784, 0x95BA2A53F983CF38, // q = 155
                0x952AB45CFA97A0B2, 0xDD945A747BF26183, // q = 156
                0xBA756174393D88DF, 0x94F971119AEEF9E4, // q = 157
                0xE912B9D1478CEB17, 0x7A37CD5601AAB85D, // q = 158
                0x91ABB422CCB812EE, 0xAC62E055C10AB33A, // q = 159
                0xB616A12B7FE617AA, 0x577B986B314D6009, // q = 160
                0xE39C49765FDF9D94, 0xED5A7E85FDA0B80B, // q = 161
                0x8E41ADE9FBEBC27D, 0x14588F13BE847307, // q = 162
                0xB1D219647AE6B31C, 0x596EB2D8AE258FC8, // q = 163
                0xDE469FBD99A05FE3, 0x6FCA5F8ED9AEF3BB, // q = 164
                0x8AEC23D680043BEE, 0x25DE7BB9480D5854, // q = 165
                0xADA72CCC20054AE9, 0xAF561AA79A10AE6A, // q = 166
                0xD910F7FF28069DA4, 0x1B2BA1518094DA04, // q = 167
                0x87AA9AFF79042286, 0x90FB44D2F05D0842, // q = 168
                0xA99541BF57452B28, 0x353A1607AC744A53, // q = 169
                0xD3FA922F2D1675F2, 0x42889B8997915CE8, // q = 170
                0x847C9B5D7C2E09B7, 0x69956135FEBADA11, // q = 171
                0xA59BC234DB398C25, 0x43FAB9837E699095, // q = 172
                0xCF02B2C21207EF2E, 0x94F967E45E03F4BB, // q = 173
                0x8161AFB94B44F57D, 0x1D1BE0EEBAC278F5, // q = 174
                0xA1BA1BA79E1632DC, 0x6462D92A69731732, // q = 175
                0xCA28A291859BBF93, 0x7D7B8F7503CFDCFE, // q = 176
                0xFCB2CB35E702AF78, 0x5CDA735244C3D43E, // q = 177
                0x9DEFBF01B061ADAB, 0x3A0888136AFA64A7, // q = 178
                0xC56BAEC21C7A1916, 0x088AAA1845B8FDD0, // q = 179
                0xF6C69A72A3989F5B, 0x8AAD549E57273D45, // q = 180
                0x9A3C2087A63F6399, 0x36AC54E2F678864B, // q = 181
                0xC0CB28A98FCF3C7F, 0x84576A1BB416A7DD, // q = 182
                0xF0FDF2D3F3C30B9F, 0x656D44A2A11C51D5, // q = 183
                0x969EB7C47859E743, 0x9F644AE5A4B1B325, // q = 184
                0xBC4665B596706114, 0x873D5D9F0DDE1FEE, // q = 185
                0xEB57FF22FC0C7959, 0xA90CB506D155A7EA, // q = 186
                0x9316FF75DD87CBD8, 0x09A7F12442D588F2, // q = 187
                0xB7DCBF5354E9BECE, 0x0C11ED6D538AEB2F, // q = 188
                0xE5D3EF282A242E81, 0x8F1668C8A86DA5FA, // q = 189
                0x8FA475791A569D10, 0xF96E017D694487BC, // q = 190
                0xB38D92D760EC4455, 0x37C981DCC395A9AC, // q = 191
                0xE070F78D3927556A, 0x85BBE253F47B1417, // q = 192
                0x8C469AB843B89562, 0x93956D7478CCEC8E, // q = 193
                0xAF58416654A6BABB, 0x387AC8D1970027B2, // q = 194
                0xDB2E51BFE9D0696A, 0x06997B05FCC0319E, // q = 195
                0x88FCF317F22241E2, 0x441FECE3BDF81F03, // q = 196
                0xAB3C2FDDEEAAD25A, 0xD527E81CAD7626C3, // q = 197
                0xD60B3BD56A5586F1, 0x8A71E223D8D3B074, // q = 198
                0x85C7056562757456, 0xF6872D5667844E49, // q = 199
                0xA738C6BEBB12D16C, 0xB428F8AC016561DB, // q = 200
                0xD106F86E69D785C7, 0xE13336D701BEBA52, // q = 201
                0x82A45B450226B39C, 0xECC0024661173473, // q = 202
                0xA34D721642B06084, 0x27F002D7F95D0190, // q = 203
                0xCC20CE9BD35C78A5, 0x31EC038DF7B441F4, // q = 204
                0xFF290242C83396CE, 0x7E67047175A15271, // q = 205
                0x9F79A169BD203E41, 0x0F0062C6E984D386, // q = 206
                0xC75809C42C684DD1, 0x52C07B78A3E60868, // q = 207
                0xF92E0C3537826145, 0xA7709A56CCDF8A82, // q = 208
                0x9BBCC7A142B17CCB, 0x88A66076400BB691, // q = 209
                0xC2ABF989935DDBFE, 0x6ACFF893D00EA435, // q = 210
                0xF356F7EBF83552FE, 0x0583F6B8C4124D43, // q = 211
                0x98165AF37B2153DE, 0xC3727A337A8B704A, // q = 212
                0xBE1BF1B059E9A8D6, 0x744F18C0592E4C5C, // q = 213
                0xEDA2EE1C7064130C, 0x1162DEF06F79DF73, // q = 214
                0x9485D4D1C63E8BE7, 0x8ADDCB5645AC2BA8, // q = 215
                0xB9A74A0637CE2EE1, 0x6D953E2BD7173692, // q = 216
                0xE8111C87C5C1BA99, 0xC8FA8DB6CCDD0437, // q = 217
                0x910AB1D4DB9914A0, 0x1D9C9892400A22A2, // q = 218
                0xB54D5E4A127F59C8, 0x2503BEB6D00CAB4B, // q = 219
                0xE2A0B5DC971F303A, 0x2E44AE64840FD61D, // q = 220
                0x8DA471A9DE737E24, 0x5CEAECFED289E5D2, // q = 221
                0xB10D8E1456105DAD, 0x7425A83E872C5F47, // q = 222
                0xDD50F1996B947518, 0xD12F124E28F77719, // q = 223
                0x8A5296FFE33CC92F, 0x82BD6B70D99AAA6F, // q = 224
                0xACE73CBFDC0BFB7B, 0x636CC64D1001550B, // q = 225
                0xD8210BEFD30EFA5A, 0x3C47F7E05401AA4E, // q = 226
                0x8714A775E3E95C78, 0x65ACFAEC34810A71, // q = 227
                0xA8D9D1535CE3B396, 0x7F1839A741A14D0D, // q = 228
                0xD31045A8341CA07C, 0x1EDE48111209A050, // q = 229
                0x83EA2B892091E44D, 0x934AED0AAB460432, // q = 230
                0xA4E4B66B68B65D60, 0xF81DA84D5617853F, // q = 231
                0xCE1DE40642E3F4B9, 0x36251260AB9D668E, // q = 232
                0x80D2AE83E9CE78F3, 0xC1D72B7C6B426019, // q = 233
                0xA1075A24E4421730, 0xB24CF65B8612F81F, // q = 234
                0xC94930AE1D529CFC, 0xDEE033F26797B627, // q = 235
                0xFB9B7CD9A4A7443C, 0x169840EF017DA3B1, // q = 236
                0x9D412E0806E88AA5, 0x8E1F289560EE864E, // q = 237
                0xC491798A08A2AD4E, 0xF1A6F2BAB92A27E2, // q = 238
                0xF5B5D7EC8ACB58A2, 0xAE10AF696774B1DB, // q = 239
                0x9991A6F3D6BF1765, 0xACCA6DA1E0A8EF29, // q = 240
                0xBFF610B0CC6EDD3F, 0x17FD090A58D32AF3, // q = 241
                0xEFF394DCFF8A948E, 0xDDFC4B4CEF07F5B0, // q = 242
                0x95F83D0A1FB69CD9, 0x4ABDAF101564F98E, // q = 243
                0xBB764C4CA7A4440F, 0x9D6D1AD41ABE37F1, // q = 244
                0xEA53DF5FD18D5513, 0x84C86189216DC5ED, // q = 245
                0x92746B9BE2F8552C, 0x32FD3CF5B4E49BB4, // q = 246
                0xB7118682DBB66A77, 0x3FBC8C33221DC2A1, // q = 247
                0xE4D5E82392A40515, 0x0FABAF3FEAA5334A, // q = 248
                0x8F05B1163BA6832D, 0x29CB4D87F2A7400E, // q = 249
                0xB2C71D5BCA9023F8, 0x743E20E9EF511012, // q = 250
                0xDF78E4B2BD342CF6, 0x914DA9246B255416, // q = 251
                0x8BAB8EEFB6409C1A, 0x1AD089B6C2F7548E, // q = 252
                0xAE9672ABA3D0C320, 0xA184AC2473B529B1, // q = 253
                0xDA3C0F568CC4F3E8, 0xC9E5D72D90A2741E, // q = 254
                0x8865899617FB1871, 0x7E2FA67C7A658892, // q = 255
                0xAA7EEBFB9DF9DE8D, 0xDDBB901B98FEEAB7, // q = 256
                0xD51EA6FA85785631, 0x552A74227F3EA565, // q = 257
                0x8533285C936B35DE, 0xD53A88958F87275F, // q = 258
                0xA67FF273B8460356, 0x8A892ABAF368F137, // q = 259
                0xD01FEF10A657842C, 0x2D2B7569B0432D85, // q = 260
                0x8213F56A67F6B29B, 0x9C3B29620E29FC73, // q = 261
                0xA298F2C501F45F42, 0x8349F3BA91B47B8F, // q = 262
                0xCB3F2F7642717713, 0x241C70A936219A73, // q = 263
                0xFE0EFB53D30DD4D7, 0xED238CD383AA0110, // q = 264
                0x9EC95D1463E8A506, 0xF4363804324A40AA, // q = 265
                0xC67BB4597CE2CE48, 0xB143C6053EDCD0D5, // q = 266
                0xF81AA16FDC1B81DA, 0xDD94B7868E94050A, // q = 267
                0x9B10A4E5E9913128, 0xCA7CF2B4191C8326, // q = 268
                0xC1D4CE1F63F57D72, 0xFD1C2F611F63A3F0, // q = 269
                0xF24A01A73CF2DCCF, 0xBC633B39673C8CEC, // q = 270
                0x976E41088617CA01, 0xD5BE0503E085D813, // q = 271
                0xBD49D14AA79DBC82, 0x4B2D8644D8A74E18, // q = 272
                0xEC9C459D51852BA2, 0xDDF8E7D60ED1219E, // q = 273
                0x93E1AB8252F33B45, 0xCABB90E5C942B503, // q = 274
                0xB8DA1662E7B00A17, 0x3D6A751F3B936243, // q = 275
                0xE7109BFBA19C0C9D, 0x0CC512670A783AD4, // q = 276
                0x906A617D450187E2, 0x27FB2B80668B24C5, // q = 277
                0xB484F9DC9641E9DA, 0xB1F9F660802DEDF6, // q = 278
                0xE1A63853BBD26451, 0x5E7873F8A0396973, // q = 279
                0x8D07E33455637EB2, 0xDB0B487B6423E1E8, // q = 280
                0xB049DC016ABC5E5F, 0x91CE1A9A3D2CDA62, // q = 281
                0xDC5C5301C56B75F7, 0x7641A140CC7810FB, // q = 282
                0x89B9B3E11B6329BA, 0xA9E904C87FCB0A9D, // q = 283
                0xAC2820D9623BF429, 0x546345FA9FBDCD44, // q = 284
                0xD732290FBACAF133, 0xA97C177947AD4095, // q = 285
                0x867F59A9D4BED6C0, 0x49ED8EABCCCC485D, // q = 286
                0xA81F301449EE8C70, 0x5C68F256BFFF5A74, // q = 287
                0xD226FC195C6A2F8C, 0x73832EEC6FFF3111, // q = 288
                0x83585D8FD9C25DB7, 0xC831FD53C5FF7EAB, // q = 289
                0xA42E74F3D032F525, 0xBA3E7CA8B77F5E55, // q = 290
                0xCD3A1230C43FB26F, 0x28CE1BD2E55F35EB, // q = 291
                0x80444B5E7AA7CF85, 0x7980D163CF5B81B3, // q = 292
                0xA0555E361951C366, 0xD7E105BCC332621F, // q = 293
                0xC86AB5C39FA63440, 0x8DD9472BF3FEFAA7, // q = 294
                0xFA856334878FC150, 0xB14F98F6F0FEB951, // q = 295
                0x9C935E00D4B9D8D2, 0x6ED1BF9A569F33D3, // q = 296
                0xC3B8358109E84F07, 0x0A862F80EC4700C8, // q = 297
                0xF4A642E14C6262C8, 0xCD27BB612758C0FA, // q = 298
                0x98E7E9CCCFBD7DBD, 0x8038D51CB897789C, // q = 299
                0xBF21E44003ACDD2C, 0xE0470A63E6BD56C3, // q = 300
                0xEEEA5D5004981478, 0x1858CCFCE06CAC74, // q = 301
                0x95527A5202DF0CCB, 0x0F37801E0C43EBC8, // q = 302
                0xBAA718E68396CFFD, 0xD30560258F54E6BA, // q = 303
                0xE950DF20247C83FD, 0x47C6B82EF32A2069, // q = 304
                0x91D28B7416CDD27E, 0x4CDC331D57FA5441, // q = 305
                0xB6472E511C81471D, 0xE0133FE4ADF8E952, // q = 306
                0xE3D8F9E563A198E5, 0x58180FDDD97723A6, // q = 307
                0x8E679C2F5E44FF8F, 0x570F09EAA7EA7648, // q = 308
                0xB201833B35D63F73, 0x2CD2CC6551E513DA, // q = 309
                0xDE81E40A034BCF4F, 0xF8077F7EA65E58D1, // q = 310
                0x8B112E86420F6191, 0xFB04AFAF27FAF782, // q = 311
                0xADD57A27D29339F6, 0x79C5DB9AF1F9B563, // q = 312
                0xD94AD8B1C7380874, 0x18375281AE7822BC, // q = 313
                0x87CEC76F1C830548, 0x8F2293910D0B15B5, // q = 314
                0xA9C2794AE3A3C69A, 0xB2EB3875504DDB22, // q = 315
                0xD433179D9C8CB841, 0x5FA60692A46151EB, // q = 316
                0x849FEEC281D7F328, 0xDBC7C41BA6BCD333, // q = 317
                0xA5C7EA73224DEFF3, 0x12B9B522906C0800, // q = 318
                0xCF39E50FEAE16BEF, 0xD768226B34870A00, // q = 319
                0x81842F29F2CCE375, 0xE6A1158300D46640, // q = 320
                0xA1E53AF46F801C53, 0x60495AE3C1097FD0, // q = 321
                0xCA5E89B18B602368, 0x385BB19CB14BDFC4, // q = 322
                0xFCF62C1DEE382C42, 0x46729E03DD9ED7B5, // q = 323
                0x9E19DB92B4E31BA9, 0x6C07A2C26A8346D1, // q = 324
                0xC5A05277621BE293, 0xC7098B7305241885, // q = 325
                0xF70867153AA2DB38, 0xB8CBEE4FC66D1EA7, // q = 326
                0x9A65406D44A5C903, 0x737F74F1DC043328, // q = 327
                0xC0FE908895CF3B44, 0x505F522E53053FF2, // q = 328
                0xF13E34AABB430A15, 0x647726B9E7C68FEF, // q = 329
                0x96C6E0EAB509E64D, 0x5ECA783430DC19F5, // q = 330
                0xBC789925624C5FE0, 0xB67D16413D132072, // q = 331
                0xEB96BF6EBADF77D8, 0xE41C5BD18C57E88F, // q = 332
                0x933E37A534CBAAE7, 0x8E91B962F7B6F159, // q = 333
                0xB80DC58E81FE95A1, 0x723627BBB5A4ADB0, // q = 334
                0xE61136F2227E3B09, 0xCEC3B1AAA30DD91C, // q = 335
                0x8FCAC257558EE4E6, 0x213A4F0AA5E8A7B1, // q = 336
                0xB3BD72ED2AF29E1F, 0xA988E2CD4F62D19D, // q = 337
                0xE0ACCFA875AF45A7, 0x93EB1B80A33B8605, // q = 338
                0x8C6C01C9498D8B88, 0xBC72F130660533C3, // q = 339
                0xAF87023B9BF0EE6A, 0xEB8FAD7C7F8680B4, // q = 340
                0xDB68C2CA82ED2A05, 0xA67398DB9F6820E1, // q = 341
            ];
        }
    }
}
