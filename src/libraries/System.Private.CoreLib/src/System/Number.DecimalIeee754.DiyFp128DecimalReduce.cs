// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Numerics;

namespace System;

internal static partial class Number
{
    // Decimal-domain Payne-Hanek argument reduction for the radian trigonometric functions, based on the
    // reduction Intel performs in the Decimal Floating-Point Math Library's `bid128_sin` / `bid64_sin`
    // (`bid_trig.c`) before the binary polynomial evaluation.
    // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
    //
    // Licensed under the BSD 3-Clause "New" or "Revised" License
    // See THIRD-PARTY-NOTICES.TXT for the full license text
    //
    // The binary Payne-Hanek in `DiyFp128RadianReduce` reduces the binary128 approximation of the
    // argument. That is exact only while the decimal operand converts to binary128 without rounding
    // (a coefficient C times 10^e is exact iff C * 5^e fits in the 128-bit significand, i.e. e <= 55 for
    // C == 1). Beyond that the conversion drops the low digits that determine `x mod 2*pi`, so a large
    // inexact argument would reduce a corrupted value. This reducer instead forms `frac(x / (2*pi))`
    // directly from the decimal coefficient and a stored binary expansion of 1/(2*pi), so it is accurate
    // for every finite magnitude. Its fraction words feed the same `DiyFp128FinishRadianReduce` tail as
    // the binary path, so both produce an identical (quadrant, reduced) contract.

    // Number of 64-bit fraction words of frac(x / (2*pi)) produced; the tail consumes the top four.
    private const int TrigReduceWords = 8;

    // Stack budget for the little-endian words of C * 10^e; 128 words == 1 KB keeps the common exponent
    // range on the stack, and larger magnitudes (up to ~336 words at the Decimal128 maximum) rent instead.
    private const int TrigReduceStackNWords = 128;

    // The leading 340 x 64-bit words of the pure binary fraction 1/(2*pi) (word 0 = bits 2^-1..2^-64).
    // 340 words span the Decimal128 maximum exponent (~2^20408) with room for the reduced significand.
    private static ReadOnlySpan<ulong> TrigOneOverTwoPi =>
    [
        0x28BE60DB9391054A, 0x7F09D5F47D4D3770, 0x36D8A5664F10E410, 0x7F9458EAF7AEF158, 0x6DC91B8E909374B8, 0x01924BBA82746487,
        0x3F877AC72C4A69CF, 0xBA208D7D4BAED121, 0x3A671C09AD17DF90, 0x4E64758E60D4CE7D, 0x272117E2EF7E4A0E, 0xC7FE25FFF7816603,
        0xFBCBC462D6829B47, 0xDB4D9FB3C9F2C26D, 0xD3D18FD9A797FA8B, 0x5D49EEB1FAF97C5E, 0xCF41CE7DE294A4BA, 0x9AFED7EC47E35742,
        0x1580CC11BF1EDAEA, 0xFC33EF0826BD0D87, 0x6A78E45857B986C2, 0x19666157C5281A10, 0x237FF620135CC9CC, 0x41818555B29CEA32,
        0x58389EF0231AD1F1, 0x0670D9F3773A024A, 0xA0D6711DA2E58729, 0xB76BD13455C6414F, 0xA97FC1C14FDF8CFA, 0x0CB0B793E60C9F6E,
        0xF0CF49BBDAC797BE, 0x27CE87CD72BC9FC7, 0x61FC48641F1F091A, 0xBE9BB55DCB4C10CE, 0xC571852D674670F0, 0xB12B50534B174003,
        0x119F618B5C78E6B1, 0xA6C0188CDF34AD25, 0xE9ED35554DFD8FB5, 0xC60428FF1D934AA7, 0x592AF5DC3E1F18D5, 0xEC1EB9C545D59270,
        0x36758ECE2129F2C8, 0xC91DE2B588D516AE, 0x47C006C2BC77F386, 0x7FCC67DA87999855, 0xE651FEEB361FDFAD, 0xD948A27A0C982FF9,
        0xB3713BC24D9B350F, 0xD775F785B78ED624, 0xA6F78A08B4BA218A, 0x1356388CB2B185B8, 0xC232DF78143005E9, 0xC77CD6F8060D04CB,
        0x9884A0C05220D6E3, 0xBD5FEC2B7CBA4790, 0xD29234D9C436376A, 0x9097EBB3985AA90A, 0x02AD2674FCA9819F, 0xDDD720F0A8E20F18,
        0x5E1CE296A32BEF75, 0xDBD8E98B72EFFD3B, 0xE06359F049917295, 0x4DB672B4AA0A2358, 0x709DF24485098126, 0xD184B11671113172,
        0x246C937CC5C02B50, 0xF539524A44357F7F, 0x2F80332507BBB39C, 0x3D4F84E03C7B30F9, 0xECCA3E31E50164CF, 0x9C706CC24BBCD142,
        0xE704A21EC82AE7ED, 0x4BB0A491CBCC9EDB, 0x55432429DC87F9DA, 0xE5B2CC52859E789E, 0x506277FD25E53A21, 0x39B8A5CC665AFB62,
        0x0D97D7C3BF6EED26, 0x921B2919D09C9C4C, 0x97636E0567C2796F, 0x094C634E5D3DC701, 0x4C0043035A0212D6, 0x3B8B242A91C0B9DD,
        0x0935AF699F7DDC92, 0x1BBBC5A7E9A523BD, 0xA46D1454F47C82B3, 0xCCE6081F92FD5A18, 0xEC97CFB740D7501F, 0xE2614A549570190D,
        0xC4361B4C920C9D53, 0x16F51C539B951170, 0x4242DA7D4AB55985, 0x2741C9D4011776CE, 0xED315DBA85FE61DF, 0x5AD26E89C74A5A65,
        0xAB333195052B5AB8, 0xA4227662141C8B2F, 0xA9012501DDDC0C3C, 0xC9FF002A1C7A9270, 0x998F781920F765E5, 0xCFE8FF6510E32183,
        0x77904C674E64A31C, 0x3779EDC5CEF7C20A, 0xCDC568201724E016, 0xA48444363A03EBE0, 0x1B12FFF6C3E40E1D, 0x8616456958AEF2D8,
        0x6E6271EF5004013C, 0xB489DD527DADBAEE, 0xC8B6EA85028BC9A2, 0x5DA0D90CCEC246A5, 0x03AA8E9470A8C76B, 0xBB6BC4899713709B,
        0x671E8B65D5B020CF, 0xC0FDBC0263100AE6, 0x4C5B41ED0E454803, 0x16F0F63124BD52EB, 0x71A97293B34DE9CD, 0xAA79A524AADA10B7,
        0x7798C67BE31D94A2, 0xDA0DF6FF2AE86B8C, 0x4577E86B8036BEC3, 0x1993592DC17B4C19, 0x4A6FD595CEBFD1EE, 0x7E5ABCEF9D77E4CA,
        0x0C202AFDA3198572, 0xC10188BE87793669, 0x2CCF63C6D5C2734D, 0xBA5093A92F84ED48, 0xCCC6AABC2A1953E9, 0x707483CFC2F35E16,
        0xDDBE48C122DEDC85, 0xE254E9B1B89B9BC0, 0x3AFBD612A6EDF6B1, 0x2E99AAB3F3DD8740, 0xB44B7C6C7066631D, 0xEB70F69221A8177D,
        0xFD20318BFC2B26BB, 0x376F170FDB77B407, 0xF1E42DB6CA8E8968, 0xE6ABC024D4EB4115, 0xEDAD0B4A5FA012E9, 0xC1F683AA9DA8565E,
        0xCA84858B6DF73F79, 0x7EBFB6E27F6FA25B, 0x1DB93F2A419C200F, 0x855BA17FE1FF41CF, 0x8A0CD9D861860ABA, 0xAF536BF9ECDB9B63,
        0xCE59E556EFCC5235, 0xE105B7CC10CB71CD, 0x5849739C326E32CC, 0x3F5B2FE88029391B, 0x0168375691DBC874, 0x8498A1172E52585C,
        0x38159AC054A64DD5, 0x542DF547B13C4CD7, 0xDB84F90C176A4BA1, 0x70EC874D8CA8692D, 0xC2352C7A887DC5B9, 0x1A63DDFFC9E000C3,
        0x0B5023683353E669, 0x4834E8ACC2974BD0, 0xBE6D32F684742F9F, 0x7076E6EF45EAE068, 0xB2971A8205D54B95, 0x4009FC051FE181F8,
        0x5902C5235065B7AF, 0xA1CABF76AD895ACD, 0x225EFFBCC167AFEE, 0x53DA9A2A0A9296B1, 0x13EF3E0B6616B5E5, 0x71FD235343698E88,
        0x17D5E92C4FC5254E, 0x2000483321B75C6D, 0xB7B27D582FC45953, 0x5AC1C06B2C233430, 0x2C92155443BEC7B0, 0xDCA54EC1A8CD5030,
        0x1EF701B311783E8A, 0x53B232B5907CFA37, 0x991F361926CC6FB6, 0x70E5E935161DF178, 0xDA44F6BC0F0EAE91, 0x861197DD557D6F74,
        0xB1A49B974BAB3B51, 0x03908F8721F1187A, 0x7F4A7CF5B9F29F08, 0x8D645BF178022375, 0xFFF89A9BB1BF6C30, 0x4224DD175F2CAB5A,
        0xE75BB35EDC8F9A84, 0x71AA73FDF7DCCA6E, 0xB26D54402DC36CB8, 0x892E9D181F7962B6, 0x1D0B054343062065, 0x199F858A405D9EA7,
        0xEFBF7F7BD1558D9F, 0xB644F67B2E6EA2FF, 0x25F109EA0C70DBBC, 0x4DB16515AA362D6A, 0x2D03B333CB62448D, 0x15DBE2558B38F3A6,
        0x6E4835AA979AE70A, 0x8FB317C45282FF7E, 0xFD385B4EE38B21B8, 0xA1353A6A6D3F347B, 0xBBF24D4B984E4BD1, 0x084E323646C2BF20,
        0x5A92BEF6070BE12D, 0x14E32653B3089537, 0x154AB5B1B0258642, 0xEE1C0699255A5816, 0x89BB948FC3C45FC4, 0x6D7D3D72FF0B6F0D,
        0x3BAF0D33177A1817, 0xB766E399FBCCE4AE, 0x05F266D6186F15F8, 0x71A0D4440FB6121C, 0x7777470B68462BD1, 0x8B0875FCD6661EB6,
        0x701527BEA193FF01, 0x95AB9E794D88A248, 0xAB4E3724D9EABA15, 0x4E09A0A6F9F2A903, 0x546C4CE643B5EA52, 0x015A7C2C9969E21F,
        0xE5D3220DB47E6CE4, 0x8852A09EC873E637, 0x27D01551F70E9D38, 0x50BAD9F7E77F97F5, 0x17A919DEDEAB2EA8, 0xBD9548E20AD56E90,
        0x421B96618A8860D1, 0xCE79B8E27527B950, 0x3ED27A55BFF283C7, 0x2296714AFEA53170, 0x74F3F143EB96B6E1, 0xB151D890E14EE188,
        0x651E4B21D8441ED3, 0x0A868B2004AFD0E4, 0x09A2224F1E39312A, 0x1EF6F9708EB13ABD, 0x09A299FDEFE4834A, 0xE8D96C64CF42DF2F,
        0x77146918F749F778, 0x5A466526A54A6A0A, 0x339A2D3B424827D1, 0x32A61398E09C08DF, 0x1F8CAE43E3BD69F9, 0xD585023C484AA76D,
        0x535F9BD446696AFE, 0x6D75B7E098776580, 0x8D85A7CEB12868A0, 0xDB7B5C9EA34E6A6E, 0x20970C9AD6C9D1BB, 0x4D001DC034957D3F,
        0x135640601C78384F, 0xE26CA57CD92A3C6B, 0xA9D2CE3F133AACAE, 0xD1C9C2EAF0E9CD2E, 0x9814B74D3E158EBA, 0xDFA28C6ECD96256D,
        0xD1FDEA6530EAB4E4, 0x89479FCFB625D3AE, 0xEA53F62B8079986D, 0x0E4F63A948EA8CC1, 0xA3858CED4EEC6207, 0x4EA75004F43306F9,
        0x7E18B9CEFCA3CE6D, 0x6FC2F08D489D1FA8, 0x91F0354B47C66B74, 0xE42537E4C4742D0A, 0xC9525B6CB8992C97, 0xBC4D4EF1A90692B4,
        0x2AB24B993A2195CC, 0x24660B3ECC46C682, 0x1CA2EF73B8583850, 0xBAD907742EE8F956, 0x75165EE30A9120FC, 0xCCAEBE1219CB2346,
        0xCBEA6C143CF77E7D, 0x5CF6D86D3F88CF9B, 0x1069BBA8C61DD689, 0xAF179733A9C22537, 0x15F88065BC7A0E6F, 0x9214574B4BD3A555,
        0x765BB0B9F5D558C1, 0x38300B83BF10282E, 0xFE6CDC4969C88B7E, 0xFD867620E3071986, 0x79AC83556ED44DDE, 0x7A026BE452435CF7,
        0x82C369739FD62B06, 0x4D1C9199DE8684E7, 0x89AF115579D6172D, 0x5C4745121A645203, 0x5815AB6AF58BD925, 0xBB83084BFCD75B62,
        0x299DA1255947AAE7, 0x829377BF95C40420, 0xDA8E7E3A8C678E07, 0x7AB22C72B25ACDFC, 0x87B5417A6611D0E7, 0xF15B00CC6DCEE2FE,
        0x21B95AA370D0D88C, 0x39E4F3F55AA3CB5C, 0xCC0146BC086827F2, 0xDD0568755AC8DBFD, 0xC94BD2F1EE29645F, 0xEB16571577884B0E,
        0x2C4CA5973FD40D98, 0x98BE9EC5BD36698A, 0xB3F9FC1D00F53581, 0x1BF6458C6C6FF2ED, 0x1416F5F20338651A, 0xF590D3F64737D150,
        0xD7CD14F8AD6AB26B, 0xB204C5217E74AFEE, 0xB6E79DBD6E6BC573, 0xF28C60852D5B7A7F, 0x93543F0D7D6BB568, 0xB430725815C64BAD,
        0xBA476481F4512BA8, 0xF18D0D5989B56D0C, 0x58788DFC688827BF, 0xE56388D24DE60D7D, 0x2992F700B0AF84EF, 0xA02802DCA8C45717,
        0xC786F4436D34E1A7, 0xA165A5DACAB247E2, 0x89B08C1C3C010504, 0xBF27E97DCA8E271A, 0x1E38AA9D9433F855, 0x649D24D38E02A4BD,
        0xD54CFC298F6D0E66, 0x5C78ADD56A629F00, 0x23C66B15348BA82D, 0x3D7CF81832127FF4,
    ];

    /// <summary>
    /// Reports whether the finite decimal <c>C * 10^e</c> converts to the binary128 engine exactly (no
    /// rounding). When it does, the binary Payne-Hanek reduction is exact for any magnitude, so the
    /// argument stays on that proven path; otherwise the decimal-domain reducer is used.
    /// </summary>
    private static bool DiyFp128DecimalReduceExact(UInt128 coefficient, int unbiasedExponent)
    {
        if (unbiasedExponent >= 0)
        {
            // The 2^e factor is just a binary exponent, so exactness needs C * 5^e < 2^128.
            if (unbiasedExponent >= 56)
            {
                return false; // 5^56 > 2^128
            }

            UInt128 pow5 = DiyFp128Pow5(unbiasedExponent);
            return coefficient <= (UInt128.MaxValue / pow5);
        }

        int k = -unbiasedExponent;
        if (k >= 55)
        {
            return false; // 5^55 > 2^127 >= any coefficient
        }

        // C / 10^k = C / (2^k * 5^k) is exact in binary iff 5^k divides C.
        return (coefficient % DiyFp128Pow5(k)) == UInt128.Zero;
    }

    private static UInt128 DiyFp128Pow5(int n)
    {
        UInt128 result = UInt128.One;
        for (int i = 0; i < n; i++)
        {
            result *= 5;
        }
        return result;
    }

    /// <summary>
    /// Reduces the finite non-zero magnitude <c>coefficient * 10^unbiasedExponent</c> (with the given
    /// <paramref name="sign"/>) against 2*pi in the decimal domain, returning the quadrant (0..3) and the
    /// signed reduced argument in <c>[-pi/4, pi/4]</c>. <paramref name="octant"/> matches the binary
    /// reducer's argument (0 for sin/tan/sincos, 2 for cos).
    /// </summary>
    private static int DiyFp128DecimalRadianReduce(uint sign, UInt128 coefficient, int unbiasedExponent, int octant, out DiyFp128 reduced)
    {
        Span<ulong> fractionWords = stackalloc ulong[TrigReduceWords];

        if (unbiasedExponent >= 0)
        {
            // C * 10^power grows by at most one 64-bit word per 19-digit chunk (10^19 < 2^64), starting
            // from the two words of C, so this bounds the little-endian word count exactly. Keep the
            // common range on the stack and rent only the rare extreme-magnitude buffers.
            int maxNWords = 2 + ((unbiasedExponent + 18) / 19);

            ulong[]? rented = null;
            Span<ulong> nWords = (maxNWords <= TrigReduceStackNWords)
                ? stackalloc ulong[TrigReduceStackNWords]
                : (rented = ArrayPool<ulong>.Shared.Rent(maxNWords));

            int nCount = DiyFp128ReduceBuildN(coefficient, unbiasedExponent, nWords);
            DiyFp128ReduceFractionPositive(nWords[..nCount], fractionWords);

            if (rented is not null)
            {
                ArrayPool<ulong>.Shared.Return(rented);
            }
        }
        else
        {
            DiyFp128ReduceFractionNegative(coefficient, -unbiasedExponent, fractionWords);
        }

        int signX = unchecked((int)sign);

        // Fold the octant into bit 61 of the most significant word, exactly as the binary reducer does
        // (bit 63:62 hold the quadrant, so octant 2 increments the quadrant to yield cos(x) = sin(x+pi/2)).
        int octantSigned = (signX != 0) ? -octant : octant;
        unchecked
        {
            fractionWords[0] += (ulong)(long)octantSigned << 61;
        }

        return DiyFp128FinishRadianReduce(fractionWords[0], fractionWords[1], fractionWords[2], fractionWords[3], 0, signX, out reduced);
    }

    /// <summary>Builds the little-endian 64-bit words of <c>coefficient * 10^power</c> (power &gt;= 0).</summary>
    private static int DiyFp128ReduceBuildN(UInt128 coefficient, int power, Span<ulong> nWords)
    {
        nWords[0] = (ulong)coefficient;
        int count = 1;

        ulong high = (ulong)(coefficient >> 64);
        if (high != 0)
        {
            nWords[1] = high;
            count = 2;
        }

        int remaining = power;
        while (remaining > 0)
        {
            int chunk = int.Min(remaining, 19);
            ulong multiplier = ulong.PowersOf10[chunk];

            UInt128 carry = 0;
            for (int i = 0; i < count; i++)
            {
                UInt128 product = ((UInt128)nWords[i] * multiplier) + carry;
                nWords[i] = (ulong)product;
                carry = product >> 64;
            }

            if (carry != 0)
            {
                nWords[count++] = (ulong)carry;
            }

            remaining -= chunk;
        }

        return count;
    }

    /// <summary>
    /// Fills <paramref name="fractionWords"/> with the leading words of <c>frac(N / (2*pi))</c> for the
    /// integer N given by its little-endian words (word 0 = bits 2^-1..2^-64).
    /// </summary>
    private static void DiyFp128ReduceFractionPositive(ReadOnlySpan<ulong> nWords, Span<ulong> fractionWords)
    {
        const int Guard = 3;
        int columns = fractionWords.Length + Guard;

        // Each column t sums Nw[j] * Fw[j+t]; the sum can exceed 128 bits, so carry the overflow count.
        Span<UInt128> columnLo = stackalloc UInt128[TrigReduceWords + Guard];
        Span<ulong> columnHi = stackalloc ulong[TrigReduceWords + Guard];

        int tableLength = TrigOneOverTwoPi.Length;

        for (int t = 0; t < columns; t++)
        {
            UInt128 acc = 0;
            ulong accHigh = 0;

            for (int j = 0; j < nWords.Length; j++)
            {
                int fi = j + t;
                if (fi >= tableLength)
                {
                    break;
                }

                UInt128 next = acc + ((UInt128)nWords[j] * TrigOneOverTwoPi[fi]);
                if (next < acc)
                {
                    accHigh++;
                }
                acc = next;
            }

            columnLo[t] = acc;
            columnHi[t] = accHigh;
        }

        DiyFp128ReducePropagate(columnLo, columnHi, columns, fractionWords);
    }

    /// <summary>
    /// Fills <paramref name="fractionWords"/> with the leading words of <c>frac(C / (2*pi))</c> when the
    /// decimal exponent is negative (C * 10^-k), forming <c>(1/(2*pi)) / 10^k</c> by long division in
    /// <c>&lt;= 10^19</c> chunks and then multiplying by the coefficient.
    /// </summary>
    private static void DiyFp128ReduceFractionNegative(UInt128 coefficient, int k, Span<ulong> fractionWords)
    {
        const int ModGuard = 8;
        int modLength = fractionWords.Length + ModGuard;

        Span<ulong> moduli = stackalloc ulong[TrigReduceWords + ModGuard];
        for (int i = 0; i < modLength; i++)
        {
            moduli[i] = TrigOneOverTwoPi[i];
        }

        int remaining = k;
        while (remaining > 0)
        {
            int chunk = int.Min(remaining, 19);
            ulong divisor = ulong.PowersOf10[chunk];

            ulong rem = 0;
            for (int i = 0; i < modLength; i++)
            {
                UInt128 cur = ((UInt128)rem << 64) | moduli[i];
                ulong q = (ulong)(cur / divisor);
                rem = (ulong)(cur - ((UInt128)q * divisor));
                moduli[i] = q;
            }

            remaining -= chunk;
        }

        // P = frac(C * moduli); C is at most two words.
        ulong c0 = (ulong)coefficient;
        ulong c1 = (ulong)(coefficient >> 64);
        int nC = (c1 != 0) ? 2 : 1;

        const int Guard = 2;
        int columns = fractionWords.Length + Guard;

        Span<UInt128> columnLo = stackalloc UInt128[TrigReduceWords + Guard];
        Span<ulong> columnHi = stackalloc ulong[TrigReduceWords + Guard];

        for (int t = 0; t < columns; t++)
        {
            UInt128 acc = 0;
            ulong accHigh = 0;

            for (int a = 0; a < nC; a++)
            {
                int b = a + t;
                if (b >= modLength)
                {
                    break;
                }

                ulong ca = (a == 0) ? c0 : c1;
                UInt128 next = acc + ((UInt128)ca * moduli[b]);
                if (next < acc)
                {
                    accHigh++;
                }
                acc = next;
            }

            columnLo[t] = acc;
            columnHi[t] = accHigh;
        }

        DiyFp128ReducePropagate(columnLo, columnHi, columns, fractionWords);
    }

    /// <summary>
    /// Carry-propagates the 192-bit per-column sums (least significant weight first) into the output
    /// fraction words, discarding the carry out of the most significant word (the integer part, mod 1).
    /// </summary>
    private static void DiyFp128ReducePropagate(ReadOnlySpan<UInt128> columnLo, ReadOnlySpan<ulong> columnHi, int columns, Span<ulong> fractionWords)
    {
        UInt128 carry = 0;

        for (int t = columns - 1; t >= 0; t--)
        {
            UInt128 lo = columnLo[t] + carry;
            ulong hiAdd = (lo < columnLo[t]) ? 1UL : 0UL;
            ulong fullHi = columnHi[t] + hiAdd;

            if (t < fractionWords.Length)
            {
                fractionWords[t] = (ulong)lo;
            }

            // carry to the next column is the top 128 bits of the 192-bit value fullHi:lo.
            carry = ((UInt128)fullHi << 64) | (lo >> 64);
        }
    }
}
