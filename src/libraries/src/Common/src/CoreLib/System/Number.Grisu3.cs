// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

namespace System
{
    internal static partial class Number
    {
        // This is a port of the `Grisu3` implementation here: https://github.com/google/double-conversion/blob/a711666ddd063eb1e4b181a6cb981d39a1fc8bac/double-conversion/fast-dtoa.cc
        // The backing algorithm and the proofs behind it are described in more detail here: http://www.cs.tufts.edu/~nr/cs257/archive/florian-loitsch/printf.pdf
        // ======================================================================================================================================== 
        //
        // Overview:
        //
        // The general idea behind Grisu3 is to leverage additional bits and cached powers of ten to generate the correct digits.
        // The algorithm is imprecise for some numbers. Fortunately, the algorithm itself can determine this scenario and gives us
        // a result indicating success or failure. We must fallback to a different algorithm for the failing scenario.
        internal static class Grisu3
        {
            private const int CachedPowersDecimalExponentDistance = 8;
            private const int CachedPowersMinDecimalExponent = -348;
            private const int CachedPowersPowerMaxDecimalExponent = 340;
            private const int CachedPowersOffset = -CachedPowersMinDecimalExponent;

            // 1 / Log2(10)
            private const double D1Log210 = 0.301029995663981195;

            // The minimal and maximal target exponents define the range of w's binary exponent,
            // where w is the result of multiplying the input by a cached power of ten.
            //
            // A different range might be chosen on a different platform, to optimize digit generation,
            // but a smaller range requires more powers of ten to be cached.
            private const int MaximalTargetExponent = -32;
            private const int MinimalTargetExponent = -60;

            private static readonly short[] s_CachedPowersBinaryExponent = new short[]
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

            private static readonly short[] s_CachedPowersDecimalExponent = new short[]
            {
                CachedPowersMinDecimalExponent,
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
                CachedPowersPowerMaxDecimalExponent,
            };

            private static readonly ulong[] s_CachedPowersSignificand = new ulong[]
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

            private static readonly uint[] s_SmallPowersOfTen = new uint[]
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

            public static bool TryRunDouble(double value, int requestedDigits, ref NumberBuffer number)
            {
                double v = double.IsNegative(value) ? -value : value;

                Debug.Assert(v > 0);
                Debug.Assert(double.IsFinite(v));

                int length = 0;
                int decimalExponent = 0;
                bool result = false;

                if (requestedDigits == -1)
                {
                    DiyFp w = DiyFp.CreateAndGetBoundaries(v, out DiyFp boundaryMinus, out DiyFp boundaryPlus).Normalize();
                    result = TryRunShortest(in boundaryMinus, in w, in boundaryPlus, number.Digits, out length, out decimalExponent);
                }
                else
                {
                    DiyFp w = new DiyFp(v).Normalize();
                    result = TryRunCounted(in w, requestedDigits, number.Digits, out length, out decimalExponent);
                }

                if (result)
                {
                    Debug.Assert((requestedDigits == -1) || (length == requestedDigits));

                    number.Scale = length + decimalExponent;
                    number.Digits[length] = (byte)('\0');
                    number.DigitsCount = length;
                }

                return result;
            }

            public static bool TryRunSingle(float value, int requestedDigits, ref NumberBuffer number)
            {
                float v = float.IsNegative(value) ? -value : value;

                Debug.Assert(v > 0);
                Debug.Assert(float.IsFinite(v));

                int length = 0;
                int decimalExponent = 0;
                bool result = false;

                if (requestedDigits == -1)
                {
                    DiyFp w = DiyFp.CreateAndGetBoundaries(v, out DiyFp boundaryMinus, out DiyFp boundaryPlus).Normalize();
                    result = TryRunShortest(in boundaryMinus, in w, in boundaryPlus, number.Digits, out length, out decimalExponent);
                }
                else
                {
                    DiyFp w = new DiyFp(v).Normalize();
                    result = TryRunCounted(in w, requestedDigits, number.Digits, out length, out decimalExponent);
                }

                if (result)
                {
                    Debug.Assert((requestedDigits == -1) || (length == requestedDigits));

                    number.Scale = length + decimalExponent;
                    number.Digits[length] = (byte)('\0');
                    number.DigitsCount = length;
                }

                return result;
            }

            // The counted version of Grisu3 only generates requestedDigits number of digits.
            // This version does not generate the shortest representation, and with enough requested digits 0.1 will at some point print as 0.9999999...
            // Grisu3 is too imprecise for real halfway cases (1.5 will not work) and therefore the rounding strategy for halfway cases is irrelevant.
            private static bool TryRunCounted(in DiyFp w, int requestedDigits, Span<byte> buffer, out int length, out int decimalExponent)
            {
                Debug.Assert(requestedDigits > 0);

                int tenMkMinimalBinaryExponent = MinimalTargetExponent - (w.e + DiyFp.SignificandSize);
                int tenMkMaximalBinaryExponent = MaximalTargetExponent - (w.e + DiyFp.SignificandSize);

                DiyFp tenMk = GetCachedPowerForBinaryExponentRange(tenMkMinimalBinaryExponent, tenMkMaximalBinaryExponent, out int mk);

                Debug.Assert(MinimalTargetExponent <= (w.e + tenMk.e + DiyFp.SignificandSize));
                Debug.Assert(MaximalTargetExponent >= (w.e + tenMk.e + DiyFp.SignificandSize));

                // Note that tenMk is only an approximation of 10^-k.
                // A DiyFp only contains a 64-bit significand and tenMk is thus only precise up to 64-bits.

                // The DiyFp.Multiply procedure rounds its result and tenMk is approximated too.
                // The variable scaledW (as well as scaledBoundaryMinus/Plus) are now off by a small amount.
                //
                // In fact, scaledW - (w * 10^k) < 1ulp (unit in last place) of scaledW.
                // In other words, let f = scaledW.f and e = scaledW.e, then:
                //      (f - 1) * 2^e < (w * 10^k) < (f + 1) * 2^e

                DiyFp scaledW = w.Multiply(in tenMk);

                // We now have (double)(scaledW * 10^-mk).
                //
                // DigitGenCounted will generate the first requestedDigits of scaledW and return together with a kappa such that:
                //      scaledW ~= buffer * 10^kappa.
                //
                // It will not always be exactly the same since DigitGenCounted only produces a limited number of digits.

                bool result = TryDigitGenCounted(in scaledW, requestedDigits, buffer, out length, out int kappa);
                decimalExponent = -mk + kappa;
                return result;
            }

            // Provides a decimal representation of v.
            // Returns true if it succeeds; otherwise, the result cannot be trusted.
            //
            // There will be length digits inside the buffer (not null-terminated).
            // If the function returns true then:
            //      v == (double)(buffer * 10^decimalExponent)
            //
            // The digits in the buffer are the shortest represenation possible (no 0.09999999999999999 instead of 0.1).
            // The shorter representation will even be chosen if the longer one would be closer to v.
            //
            // The last digit will be closest to the actual v.
            // That is, even if several digits might correctly yield 'v' when read again, the closest will be computed.
            private static bool TryRunShortest(in DiyFp boundaryMinus, in DiyFp w, in DiyFp boundaryPlus, Span<byte> buffer, out int length, out int decimalExponent)
            {
                // boundaryMinus and boundaryPlus are the boundaries between v and its closest floating-point neighbors.
                // Any number strictly between boundaryMinus and boundaryPlus will round to v when converted to a double.
                // Grisu3 will never output representations that lie exactly on a boundary.

                Debug.Assert(boundaryPlus.e == w.e);

                int tenMkMinimalBinaryExponent = MinimalTargetExponent - (w.e + DiyFp.SignificandSize);
                int tenMkMaximalBinaryExponent = MaximalTargetExponent - (w.e + DiyFp.SignificandSize);

                DiyFp tenMk = GetCachedPowerForBinaryExponentRange(tenMkMinimalBinaryExponent, tenMkMaximalBinaryExponent, out int mk);

                Debug.Assert(MinimalTargetExponent <= (w.e + tenMk.e + DiyFp.SignificandSize));
                Debug.Assert(MaximalTargetExponent >= (w.e + tenMk.e + DiyFp.SignificandSize));

                // Note that tenMk is only an approximation of 10^-k.
                // A DiyFp only contains a 64-bit significan and tenMk is thus only precise up to 64-bits.

                // The DiyFp.Multiply procedure rounds its result and tenMk is approximated too.
                // The variable scaledW (as well as scaledBoundaryMinus/Plus) are now off by a small amount.
                //
                // In fact, scaledW - (w * 10^k) < 1ulp (unit in last place) of scaledW.
                // In other words, let f = scaledW.f and e = scaledW.e, then:
                //      (f - 1) * 2^e < (w * 10^k) < (f + 1) * 2^e

                DiyFp scaledW = w.Multiply(in tenMk);
                Debug.Assert(scaledW.e == (boundaryPlus.e + tenMk.e + DiyFp.SignificandSize));

                // In theory, it would be possible to avoid some recomputations by computing the difference between w
                // and boundaryMinus/Plus (a power of 2) and to compute scaledBoundaryMinus/Plus by subtracting/adding
                // from scaledW. However, the code becomes much less readable and the speed enhancements are not terrific.

                DiyFp scaledBoundaryMinus = boundaryMinus.Multiply(in tenMk);
                DiyFp scaledBoundaryPlus = boundaryPlus.Multiply(in tenMk);

                // DigitGen will generate the digits of scaledW. Therefore, we have:
                //      v == (double)(scaledW * 10^-mk)
                //
                // Set decimalExponent == -mk and pass it to DigitGen and if scaledW is not an integer than it will be updated.
                // For instance, if scaledW == 1.23 then the buffer will be filled with "123" and the decimalExponent will be decreased by 2.

                bool result = TryDigitGenShortest(in scaledBoundaryMinus, in scaledW, in scaledBoundaryPlus, buffer, out length, out int kappa);
                decimalExponent = -mk + kappa;
                return result;
            }

            // Returns the biggest power of ten that is less than or equal to the given number.
            // We furthermore receive the maximum number of bits 'number' has.
            //
            // Returns power == 10^(exponent) such that
            //      power <= number < power * 10
            // If numberBits == 0, then 0^(0-1) is returned.
            // The number of bits must be <= 32.
            //
            // Preconditions:
            //      number < (1 << (numberBits + 1))
            private static uint BiggestPowerTen(uint number, int numberBits, out int exponentPlusOne)
            {
                // Inspired by the method for finding an integer log base 10 from here: 
                // http://graphics.stanford.edu/~seander/bithacks.html#IntegerLog10

                Debug.Assert(number < (1U << (numberBits + 1)));

                // 1233/4096 is approximately 1/log2(10)
                int exponentGuess = ((numberBits + 1) * 1233) >> 12;
                Debug.Assert((uint)(exponentGuess) < s_SmallPowersOfTen.Length);

                uint power = s_SmallPowersOfTen[exponentGuess];

                // We don't have any guarantees that 2^numberBits <= number
                if (number < power)
                {
                    exponentGuess -= 1;
                    power = s_SmallPowersOfTen[exponentGuess];
                }

                exponentPlusOne = exponentGuess + 1;
                return power;
            }

            // Generates (at most) requestedDigits of input number w.
            //
            // w is a floating-point number (DiyFp), consisting of a significand and an exponent.
            // Its exponent is bounded by MinimalTargetExponent and MaximalTargetExponent, hence:
            //      -60 <= w.e <= -32
            //
            // Returns false if it fails, in which case the generated digits in the buffer should not be used.
            //
            // Preconditions:
            //      w is correct up to 1 ulp (unit in last place). That is, its error must be strictly less than a unit of its last digit.
            //      MinimalTargetExponent <= w.e <= MaximalTargetExponent
            //
            // Postconditions:
            //      Returns false if the procedure fails; otherwise:
            //      * buffer is not null-terminated, but length contains the number of digits.
            //      * The representation in buffer is the most precise representation of requestedDigits digits.
            //      * buffer contains at most requestedDigits digits of w. If there are less than requestedDigits digits then some trailing '0's have been removed.
            //      * kappa is such that w = buffer * 10^kappa + eps with |eps| < 10^kappa / 2.
            //
            // This procedure takes into account the imprecision of its input numbers.
            // If the precision is not enough to guarantee all the postconditions, then false is returned.
            // This usually happens rarely, but the failure-rate increases with higher requestedDigits
            private static bool TryDigitGenCounted(in DiyFp w, int requestedDigits, Span<byte> buffer, out int length, out int kappa)
            {
                Debug.Assert(MinimalTargetExponent <= w.e);
                Debug.Assert(w.e <= MaximalTargetExponent);
                Debug.Assert(MinimalTargetExponent >= -60);
                Debug.Assert(MaximalTargetExponent <= -32);

                // w is assumed to have an error less than 1 unit.
                // Whenever w is scaled we also scale its error.
                ulong wError = 1;

                // We cut the input number into two parts: the integral digits and the fractional digits.
                // We don't emit any decimal separator, but adapt kapp instead.
                // For example: instead of writing "1.2", we put "12" into the buffer and increase kappa by 1.
                var one = new DiyFp((1UL << -w.e), w.e);

                // Division by one is a shift.
                uint integrals = (uint)(w.f >> -one.e);

                // Modulo by one is an and.
                ulong fractionals = w.f & (one.f - 1);

                // We deviate from the original algorithm here and do some early checks to determine if we can satisfy requestedDigits.
                // If we determine that we can't, we exit early and avoid most of the heavy lifting that the algorithm otherwise does.
                //
                // When fractionals is zero, we can easily determine if integrals can satisfy requested digits:
                //      If requestedDigits >= 11, integrals is not able to exhaust the count by itself since 10^(11 -1) > uint.MaxValue >= integrals.
                //      If integrals < 10^(requestedDigits - 1), integrals cannot exhaust the count.
                //      Otherwise, integrals might be able to exhaust the count and we need to execute the rest of the code.
                if ((fractionals == 0) && ((requestedDigits >= 11) || (integrals < s_SmallPowersOfTen[requestedDigits - 1])))
                {
                    Debug.Assert(buffer[0] == '\0');
                    length = 0;
                    kappa = 0;
                    return false;
                }

                uint divisor = BiggestPowerTen(integrals, (DiyFp.SignificandSize - (-one.e)), out kappa);
                length = 0;

                // Loop invariant:
                //      buffer = w / 10^kappa (integer division)
                // These invariants hold for the first iteration:
                //      kappa has been initialized with the divisor exponent + 1
                //      The divisor is the biggest power of ten that is smaller than integrals
                while (kappa > 0)
                {
                    uint digit = Math.DivRem(integrals, divisor, out integrals);
                    Debug.Assert(digit <= 9);
                    buffer[length] = (byte)('0' + digit);

                    length++;
                    requestedDigits--;
                    kappa--;

                    // Note that kappa now equals the exponent of the
                    // divisor and that the invariant thus holds again.
                    if (requestedDigits == 0)
                    {
                        break;
                    }

                    divisor /= 10;
                }

                if (requestedDigits == 0)
                {
                    ulong rest = ((ulong)(integrals) << -one.e) + fractionals;
                    return TryRoundWeedCounted(
                        buffer,
                        length,
                        rest,
                        tenKappa: ((ulong)(divisor)) << -one.e,
                        unit: wError,
                        ref kappa
                    );
                }

                // The integrals have been generated and we are at the point of the decimal separator.
                // In the following loop, we simply multiply the remaining digits by 10 and divide by one.
                // We just need to pay attention to multiply associated data (the unit), too.
                // Note that the multiplication by 10 does not overflow because:
                //      w.e >= -60 and thus one.e >= -60

                Debug.Assert(one.e >= MinimalTargetExponent);
                Debug.Assert(fractionals < one.f);
                Debug.Assert((ulong.MaxValue / 10) >= one.f);

                while ((requestedDigits > 0) && (fractionals > wError))
                {
                    fractionals *= 10;
                    wError *= 10;

                    // Integer division by one.
                    uint digit = (uint)(fractionals >> -one.e);
                    Debug.Assert(digit <= 9);
                    buffer[length] = (byte)('0' + digit);

                    length++;
                    requestedDigits--;
                    kappa--;

                    // Modulo by one.
                    fractionals &= (one.f - 1);
                }

                if (requestedDigits != 0)
                {
                    buffer[0] = (byte)('\0');
                    length = 0;
                    kappa = 0;
                    return false;
                }

                return TryRoundWeedCounted(
                    buffer,
                    length,
                    rest: fractionals,
                    tenKappa: one.f,
                    unit: wError,
                    ref kappa
                );
            }

            // Generates the digits of input number w.
            //
            // w is a floating-point number (DiyFp), consisting of a significand and an exponent.
            // Its exponent is bounded by kMinimalTargetExponent and kMaximalTargetExponent, hence:
            //      -60 <= w.e() <= -32.
            //
            // Returns false if it fails, in which case the generated digits in the buffer should not be used.
            //
            // Preconditions:
            //      low, w and high are correct up to 1 ulp (unit in the last place). That is, their error must be less than a unit of their last digits.
            //      low.e() == w.e() == high.e()
            //      low < w < high, and taking into account their error: low~ <= high~
            //      kMinimalTargetExponent <= w.e() <= kMaximalTargetExponent
            //
            // Postconditions:
            //      Returns false if procedure fails; otherwise:
            //      * buffer is not null-terminated, but len contains the number of digits.
            //      * buffer contains the shortest possible decimal digit-sequence such that LOW < buffer * 10^kappa < HIGH, where LOW and HIGH are the correct values of low and high (without their error).
            //      * If more than one decimal representation gives the minimal number of decimal digits then the one closest to W (where W is the correct value of w) is chosen.
            //
            // This procedure takes into account the imprecision of its input numbers.
            // If the precision is not enough to guarantee all the postconditions then false is returned.
            // This usually happens rarely (~0.5%).
            //
            // Say, for the sake of example, that:
            //      w.e() == -48, and w.f() == 0x1234567890abcdef
            //
            // w's value can be computed by w.f() * 2^w.e()
            //
            // We can obtain w's integral digits by simply shifting w.f() by -w.e().
            //      -> w's integral part is 0x1234
            //      w's fractional part is therefore 0x567890abcdef.
            //
            // Printing w's integral part is easy (simply print 0x1234 in decimal).
            //
            // In order to print its fraction we repeatedly multiply the fraction by 10 and get each digit.
            // For example, the first digit after the point would be computed by
            //      (0x567890abcdef * 10) >> 48. -> 3
            //
            // The whole thing becomes slightly more complicated because we want to stop once we have enough digits.
            // That is, once the digits inside the buffer represent 'w' we can stop.
            //
            // Everything inside the interval low - high represents w.
            // However we have to pay attention to low, high and w's imprecision.
            private static bool TryDigitGenShortest(in DiyFp low, in DiyFp w, in DiyFp high, Span<byte> buffer, out int length, out int kappa)
            {
                Debug.Assert(low.e == w.e);
                Debug.Assert(w.e == high.e);

                Debug.Assert((low.f + 1) <= (high.f - 1));

                Debug.Assert(MinimalTargetExponent <= w.e);
                Debug.Assert(w.e <= MaximalTargetExponent);

                // low, w, and high are imprecise, but by less than one ulp (unit in the last place).
                //
                // If we remove (resp. add) 1 ulp from low (resp. high) we are certain that the new numbers
                // are outside of the interval we want the final representation to lie in.
                //
                // Inversely adding (resp. removing) 1 ulp from low (resp. high) would yield numbers that
                // are certain to lie in the interval. We will use this fact later on.
                //
                // We will now start by generating the digits within the uncertain interval.
                // Later, we will weed out representations that lie outside the safe interval and thus might lie outside the correct interval.

                ulong unit = 1;

                var tooLow = new DiyFp((low.f - unit), low.e);
                var tooHigh = new DiyFp((high.f + unit), high.e);

                // tooLow and tooHigh are guaranteed to lie outside the interval we want the generated number in.

                DiyFp unsafeInterval = tooHigh.Subtract(in tooLow);

                // We now cut the input number into two parts: the integral digits and the fractional digits.
                // We will not write any decimal separator, but adapt kappa instead.
                //
                // Reminder: we are currently computing the digits (Stored inside the buffer) such that:
                //      tooLow < buffer * 10^kappa < tooHigh
                //
                // We use tooHigh for the digitGeneration and stop as soon as possible.
                // If we stop early, we effectively round down.

                var one = new DiyFp((1UL << -w.e), w.e);

                // Division by one is a shift.
                uint integrals = (uint)(tooHigh.f >> -one.e);

                // Modulo by one is an and.
                ulong fractionals = tooHigh.f & (one.f - 1);

                uint divisor = BiggestPowerTen(integrals, (DiyFp.SignificandSize - (-one.e)), out kappa);
                length = 0;

                // Loop invariant:
                //      buffer = tooHigh / 10^kappa (integer division)
                // These invariants hold for the first iteration:
                //      kappa has been initialized with the divisor exponent + 1
                //      The divisor is the biggest power of ten that is smaller than integrals
                while (kappa > 0)
                {
                    uint digit = Math.DivRem(integrals, divisor, out integrals);
                    Debug.Assert(digit <= 9);
                    buffer[length] = (byte)('0' + digit);

                    length++;
                    kappa--;

                    // Note that kappa now equals the exponent of the
                    // divisor and that the invariant thus holds again.

                    ulong rest = ((ulong)(integrals) << -one.e) + fractionals;

                    // Invariant: tooHigh = buffer * 10^kappa + DiyFp(rest, one.e)
                    // Reminder: unsafeInterval.e == one.e

                    if (rest < unsafeInterval.f)
                    {
                        // Rounding down (by not emitting the remaining digits)
                        // yields a number that lies within the unsafe interval

                        return TryRoundWeedShortest(
                            buffer,
                            length,
                            tooHigh.Subtract(w).f,
                            unsafeInterval.f,
                            rest,
                            tenKappa: ((ulong)(divisor)) << -one.e,
                            unit
                        );
                    }

                    divisor /= 10;
                }

                // The integrals have been generated and we are at the point of the decimal separator.
                // In the following loop, we simply multiply the remaining digits by 10 and divide by one.
                // We just need to pay attention to multiply associated data (the unit), too.
                // Note that the multiplication by 10 does not overflow because:
                //      w.e >= -60 and thus one.e >= -60

                Debug.Assert(one.e >= MinimalTargetExponent);
                Debug.Assert(fractionals < one.f);
                Debug.Assert((ulong.MaxValue / 10) >= one.f);

                while (true)
                {
                    fractionals *= 10;
                    unit *= 10;

                    unsafeInterval = new DiyFp((unsafeInterval.f * 10), unsafeInterval.e);

                    // Integer division by one.
                    uint digit = (uint)(fractionals >> -one.e);
                    Debug.Assert(digit <= 9);
                    buffer[length] = (byte)('0' + digit);

                    length++;
                    kappa--;

                    // Modulo by one.
                    fractionals &= (one.f - 1);

                    if (fractionals < unsafeInterval.f)
                    {
                        return TryRoundWeedShortest(
                            buffer,
                            length,
                            tooHigh.Subtract(w).f * unit,
                            unsafeInterval.f,
                            rest: fractionals,
                            tenKappa: one.f,
                            unit
                        );
                    }
                }
            }

            // Returns a cached power-of-ten with a binary exponent in the range [minExponent; maxExponent] (boundaries included).
            private static DiyFp GetCachedPowerForBinaryExponentRange(int minExponent, int maxExponent, out int decimalExponent)
            {
                Debug.Assert(s_CachedPowersSignificand.Length == s_CachedPowersBinaryExponent.Length);
                Debug.Assert(s_CachedPowersSignificand.Length == s_CachedPowersDecimalExponent.Length);

                double k = Math.Ceiling((minExponent + DiyFp.SignificandSize - 1) * D1Log210);
                int index = ((CachedPowersOffset + (int)(k) - 1) / CachedPowersDecimalExponentDistance) + 1;

                Debug.Assert((uint)(index) < s_CachedPowersSignificand.Length);

                Debug.Assert(minExponent <= s_CachedPowersBinaryExponent[index]);
                Debug.Assert(s_CachedPowersBinaryExponent[index] <= maxExponent);

                decimalExponent = s_CachedPowersDecimalExponent[index];
                return new DiyFp(s_CachedPowersSignificand[index], s_CachedPowersBinaryExponent[index]);
            }

            // Rounds the buffer upwards if the result is closer to v by possibly adding 1 to the buffer.
            // If the precision of the calculation is not sufficient to round correctly, return false.
            //
            // The rounding might shift the whole buffer, in which case, the kappy is adjusted.
            // For example "99", kappa = 3 might become "10", kappa = 4.
            //
            // If (2 * rest) > tenKappa then the buffer needs to be round up.
            // rest can have an error of +/- 1 unit.
            // This function accounts for the imprecision and returns false if the rounding direction cannot be unambiguously determined.
            //
            // Preconditions:
            //      rest < tenKappa
            private static bool TryRoundWeedCounted(Span<byte> buffer, int length, ulong rest, ulong tenKappa, ulong unit, ref int kappa)
            {
                Debug.Assert(rest < tenKappa);

                // The following tests are done in a specific order to avoid overflows.
                // They will work correctly with any ulong values of rest < tenKappa and unit.
                //
                // If the unit is too big, then we don't know which way to round.
                // For example, a unit of 50 means that the real number lies within rest +/- 50.
                // If 10^kappa == 40, then there is no way to tell which way to round.
                //
                // Even if unit is just half the size of 10^kappa we are already completely lost.
                // And after the previous test, we know that the expression will not over/underflow.
                if ((unit >= tenKappa) || ((tenKappa - unit) <= unit))
                {
                    return false;
                }

                // If 2 * (rest + unit) <= 10^kappa, we can safely round down.
                if (((tenKappa - rest) > rest) && ((tenKappa - (2 * rest)) >= (2 * unit)))
                {
                    return true;
                }

                // If 2 * (rest - unit) >= 10^kappa, we can safely round up.
                if ((rest > unit) && (tenKappa <= (rest - unit) || ((tenKappa - (rest - unit)) <= (rest - unit))))
                {
                    // Increment the last digit recursively until we find a non '9' digit.
                    buffer[length - 1]++;

                    for (int i = (length - 1); i > 0; i--)
                    {
                        if (buffer[i] != ('0' + 10))
                        {
                            break;
                        }

                        buffer[i] = (byte)('0');
                        buffer[i - 1]++;
                    }

                    // If the first digit is now '0'+10, we had a buffer with all '9's.
                    // With the exception of the first digit, all digits are now '0'.
                    // Simply switch the first digit to '1' and adjust the kappa.
                    // For example, "99" becomes "10" and the power (the kappa) is increased.
                    if (buffer[0] == ('0' + 10))
                    {
                        buffer[0] = (byte)('1');
                        kappa++;
                    }

                    return true;
                }

                return false;
            }

            // Adjusts the last digit of the generated number and screens out generated solutions that may be inaccurate.
            // A solution may be inaccurate if it is outside the safe interval or if we cannot provide that it is closer to the input than a neighboring representation of the same length.
            //
            // Input:
            //      buffer containing the digits of tooHigh / 10^kappa
            //      the buffer's length
            //      distanceTooHighW == (tooHigh - w).f * unit
            //      unsafeInterval == (tooHigh - tooLow).f * unit
            //      rest = (tooHigh - buffer * 10^kapp).f * unit
            //      tenKappa = 10^kappa * unit
            //      unit = the common multiplier
            //
            // Output:
            //      Returns true if the buffer is guaranteed to contain the closest representable number to the input.
            //
            // Modifies the generated digits in the buffer to approach (round towards) w.
            private static bool TryRoundWeedShortest(Span<byte> buffer, int length, ulong distanceTooHighW, ulong unsafeInterval, ulong rest, ulong tenKappa, ulong unit)
            {
                ulong smallDistance = distanceTooHighW - unit;
                ulong bigDistance = distanceTooHighW + unit;

                // Let wLow = tooHigh - bigDistance, and wHigh = tooHigh - smallDistance.
                //
                // Note: wLow < w < wHigh
                //
                // The real w * unit must lie somewhere inside the interval
                //      ]w_low; w_high[ (often written as "(w_low; w_high)")

                // Basically the buffer currently contains a number in the unsafe interval
                //      ]too_low; too_high[ with too_low < w < too_high
                //
                //  tooHigh - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
                //                    ^v 1 unit            ^      ^                 ^      ^
                //  boundaryHigh ---------------------     .      .                 .      .
                //                    ^v 1 unit            .      .                 .      .
                //  - - - - - - - - - - - - - - - - - - -  +  - - + - - - - - -     .      .
                //                                         .      .         ^       .      .
                //                                         .  bigDistance   .       .      .
                //                                         .      .         .       .    rest
                //                              smallDistance     .         .       .      .
                //                                         v      .         .       .      .
                //  wHigh - - - - - - - - - - - - - - - - - -     .         .       .      .
                //                    ^v 1 unit                   .         .       .      .
                //  w ---------------------------------------     .         .       .      .
                //                    ^v 1 unit                   v         .       .      .
                //  wLow  - - - - - - - - - - - - - - - - - - - - -         .       .      .
                //                                                          .       .      v
                //  buffer -------------------------------------------------+-------+--------
                //                                                          .       .
                //                                                  safeInterval    .
                //                                                          v       .
                //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -     .
                //                    ^v 1 unit                                     .
                //  boundaryLow -------------------------                     unsafeInterval
                //                    ^v 1 unit                                     v
                //  tooLow  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
                //
                //
                // Note that the value of buffer could lie anywhere inside the range tooLow to tooHigh.
                //
                // boundaryLow, boundaryHigh and w are approximations of the real boundaries and v (the input number).
                // They are guaranteed to be precise up to one unit.
                // In fact the error is guaranteed to be strictly less than one unit.
                //
                // Anything that lies outside the unsafe interval is guaranteed not to round to v when read again.
                // Anything that lies inside the safe interval is guaranteed to round to v when read again.
                //
                // If the number inside the buffer lies inside the unsafe interval but not inside the safe interval
                // then we simply do not know and bail out (returning false).
                //
                // Similarly we have to take into account the imprecision of 'w' when finding the closest representation of 'w'.
                // If we have two potential representations, and one is closer to both wLow and wHigh, then we know it is closer to the actual value v.
                //
                // By generating the digits of tooHigh we got the largest (closest to tooHigh) buffer that is still in the unsafe interval.
                // In the case where wHigh < buffer < tooHigh we try to decrement the buffer.
                // This way the buffer approaches (rounds towards) w.
                //
                // There are 3 conditions that stop the decrementation process:
                //   1) the buffer is already below wHigh
                //   2) decrementing the buffer would make it leave the unsafe interval
                //   3) decrementing the buffer would yield a number below wHigh and farther away than the current number.
                //
                // In other words:
                //      (buffer{-1} < wHigh) && wHigh - buffer{-1} > buffer - wHigh
                //
                // Instead of using the buffer directly we use its distance to tooHigh.
                //
                // Conceptually rest ~= tooHigh - buffer
                //
                // We need to do the following tests in this order to avoid over- and underflows.

                Debug.Assert(rest <= unsafeInterval);

                while ((rest < smallDistance) && ((unsafeInterval - rest) >= tenKappa) && (((rest + tenKappa) < smallDistance) || ((smallDistance - rest) >= (rest + tenKappa - smallDistance))))
                {
                    buffer[length - 1]--;
                    rest += tenKappa;
                }

                // We have approached w+ as much as possible.
                // We now test if approaching w- would require changing the buffer.
                // If yes, then we have two possible representations close to w, but we cannot decide which one is closer.
                if ((rest < bigDistance) && ((unsafeInterval - rest) >= tenKappa) && (((rest + tenKappa) < bigDistance) || ((bigDistance - rest) > (rest + tenKappa - bigDistance))))
                {
                    return false;
                }

                // Weeding test.
                //
                // The safe interval is [tooLow + 2 ulp; tooHigh - 2 ulp]
                // Since tooLow = tooHigh - unsafeInterval this is equivalent to
                //      [tooHigh - unsafeInterval + 4 ulp; tooHigh - 2 ulp]
                //
                // Conceptually we have: rest ~= tooHigh - buffer
                return ((2 * unit) <= rest) && (rest <= (unsafeInterval - 4 * unit));
            }
        }
    }
}
