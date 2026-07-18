// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System;

internal static partial class Number
{
    // This code is based on the trigonometric evaluation from the Intel(R) Decimal Floating-Point Math
    // Library, specifically `UX_SINCOS`, `UX_TANCOT` from `dpml_ux_trig.c`, the Payne-Hanek argument
    // reduction `UX_RADIAN_REDUCE` from `dpml_ux_radian_reduce.c`, the digit macros from `dpml_rdx_x.h`,
    // the rational-evaluation driver `EVALUATE_RATIONAL` from `dpml_ux_ops_64.c`, the sin/cos/tan
    // coefficient tables from `dpml_trig_x.h`, and the 4/pi table from `dpml_four_over_pi.c`.
    // Copyright (c) 2007-2025, Intel Corp. All rights reserved.
    //
    // Licensed under the BSD 3-Clause "New" or "Revised" License
    // See THIRD-PARTY-NOTICES.TXT for the full license text
    //
    // Decimal32 evaluates the trig functions in binary64; Decimal64/Decimal128 route through this
    // engine so the wider formats keep their precision.

    // ---- EVALUATE_RATIONAL flag bits (dpml_ux.h) ----
    private const int TrigPostMultiply = 0x002;
    private const int TrigSquareTerm = 0x004;
    private const int TrigAlternateSign = 0x008;
    private const int TrigNumeratorFieldWidth = 4;
    private const int TrigNoDivide = 1 << (2 * TrigNumeratorFieldWidth); // 0x100
    private const int TrigSwap = 2 << (2 * TrigNumeratorFieldWidth);     // 0x200
    private const int TrigSkip = 4 << (2 * TrigNumeratorFieldWidth);     // 0x400
    private const int TrigNumeratorMask = 0xF;
    private const int TrigDenominatorMask = 0xF << TrigNumeratorFieldWidth;

    // ODD form (numerator/sin, z*P(z^2)) and EVEN form (denominator/cos, C(z^2)).
    private const int TrigSinPolyFlags = TrigSquareTerm | TrigAlternateSign | TrigPostMultiply;
    private const int TrigCosPolyFlags = (TrigSquareTerm | TrigAlternateSign) << TrigNumeratorFieldWidth;

    private const int TrigSinCosDegree = 0xD;
    private const int TrigTanCotDegree = 0x7;

    private const int TrigSinCosFunc = 3;

    // dpml_ux.h reduction constants: UX_PRECISION, FOUR_OV_PI_ZERO_PAD_LEN, NUM_EXTRA_BITS.
    private const int TrigUxPrecision = 128;
    private const int TrigFourOverPiZeroPadLength = 138;
    private const int TrigNumExtraBits = 6;

    // Unpacked pi/4 (dpml_trig_x.h) and pi (pi/4 with binary exponent + 2).
    private static DiyFp128 TrigPiOverFour => new DiyFp128(0, 0, 0xC90FDAA22168C234, 0xC4C6628B80DC1CD1);

    private static readonly DiyFp128FixedCoefficient[] TrigSinCoefficients =
    [
        new(0x000000039E634562, 0x0000000000000000),
        new(0x000009F9DCE17A1D, 0x0000000000000000),
        new(0x001761B4083A3075, 0x0000000000000000),
        new(0x2E371DEDB1D75408, 0x0000000000000000),
        new(0xD26D1A05013C755B, 0x000000000000004B),
        new(0x1DC0C2B528320429, 0x000000000000654B),
        new(0x9CCEE07C4701FACD, 0x00000000006B9FCF),
        new(0xA1B425F28DFBF381, 0x000000005849184E),
        new(0x89C71FCE8FC76DB3, 0x00000035CC8ACFEA),
        new(0x338FAAC1C88E2826, 0x0000171DE3A556C7),
        new(0x8068068068067E8F, 0x0006806806806806),
        new(0x1111111111111106, 0x0111111111111111),
        new(0x5555555555555555, 0x1555555555555555),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    private static readonly DiyFp128FixedCoefficient[] TrigCosCoefficients =
    [
        new(0x00000061A9FB87E2, 0x0000000000000000),
        new(0x0000F96669688C7C, 0x0000000000000000),
        new(0x0219C72C77C1DE0C, 0x0000000000000000),
        new(0xCA85747F51903A09, 0x0000000000000003),
        new(0x9E18EE5EEB393833, 0x00000000000005A0),
        new(0xF9CCEE079837094C, 0x000000000006B9FC),
        new(0x301F274823772903, 0x00000000064E5D2A),
        new(0x3625ED5134A72FA4, 0x000000047BB63BFE),
        new(0xEB8E5DE02D6A41E7, 0x0000024FC9F6EF13),
        new(0xD00D00D00CFBFFE1, 0x0000D00D00D00D00),
        new(0x82D82D82D82D4910, 0x002D82D82D82D82D),
        new(0x55555555555553EB, 0x0555555555555555),
        new(0xFFFFFFFFFFFFFFFC, 0x3FFFFFFFFFFFFFFF),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    private static readonly DiyFp128FixedCoefficient[] TrigTanNumeratorCoefficients =
    [
        new(0x0000000000000000, 0x0000000000000000),
        new(0x02E36384AB86D966, 0x00000000004583DC),
        new(0xFE661C77C57437CF, 0x00000001DF2FB0D7),
        new(0xE5F9C2190062EE42, 0x0000036F46CAE26E),
        new(0x9C878717E15A162F, 0x000269DCA6FA2240),
        new(0xC5B462FFE65127B0, 0x00B209C04C0B8A2C),
        new(0xA273C25867F59A68, 0x12F6C9C3D7A587C9),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    private static readonly DiyFp128FixedCoefficient[] TrigTanDenominatorCoefficients =
    [
        new(0x196C967ACBFC02D6, 0x000000000000A9BA),
        new(0xD03D3831CF5F2FE6, 0x000000000E1AE92D),
        new(0x40A36A10FD241F97, 0x0000002E4C98A51D),
        new(0xB12A527E72E00402, 0x0000338085B96B4F),
        new(0xF5E92D642C15CBC5, 0x001739C356378673),
        new(0x7902CB9A86202DFC, 0x042C1F7EBBBFDF42),
        new(0x4D1E6D0312A04513, 0x3DA1746E82503274),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    // dpml_four_over_pi.c: the leading 263 x 64-bit digits of 4/pi (with two words of zero padding).
    private static ReadOnlySpan<ulong> TrigFourOverPi =>
    [
        0x0000000000000000, 0x0000000000000000, 0x0028BE60DB939105, 0x4A7F09D5F47D4D37, 0x7036D8A5664F10E4, 0x107F9458EAF7AEF1,
        0x586DC91B8E909374, 0xB801924BBA827464, 0x873F877AC72C4A69, 0xCFBA208D7D4BAED1, 0x213A671C09AD17DF, 0x904E64758E60D4CE,
        0x7D272117E2EF7E4A, 0x0EC7FE25FFF78166, 0x03FBCBC462D6829B, 0x47DB4D9FB3C9F2C2, 0x6DD3D18FD9A797FA, 0x8B5D49EEB1FAF97C,
        0x5ECF41CE7DE294A4, 0xBA9AFED7EC47E357, 0x421580CC11BF1EDA, 0xEAFC33EF0826BD0D, 0x876A78E45857B986, 0xC219666157C5281A,
        0x10237FF620135CC9, 0xCC41818555B29CEA, 0x3258389EF0231AD1, 0xF10670D9F3773A02, 0x4AA0D6711DA2E587, 0x29B76BD13455C641,
        0x4FA97FC1C14FDF8C, 0xFA0CB0B793E60C9F, 0x6EF0CF49BBDAC797, 0xBE27CE87CD72BC9F, 0xC761FC48641F1F09, 0x1ABE9BB55DCB4C10,
        0xCEC571852D674670, 0xF0B12B50534B1740, 0x03119F618B5C78E6, 0xB1A6C0188CDF34AD, 0x25E9ED35554DFD8F, 0xB5C60428FF1D934A,
        0xA7592AF5DC3E1F18, 0xD5EC1EB9C545D592, 0x7036758ECE2129F2, 0xC8C91DE2B588D516, 0xAE47C006C2BC77F3, 0x867FCC67DA879998,
        0x55E651FEEB361FDF, 0xADD948A27A0C982F, 0xF9B3713BC24D9B35, 0x0FD775F785B78ED6, 0x24A6F78A08B4BA21, 0x8A1356388CB2B185,
        0xB8C232DF78143005, 0xE9C77CD6F8060D04, 0xCB9884A0C05220D6, 0xE3BD5FEC2B7CBA47, 0x90D29234D9C43637, 0x6A9097EBB3985AA9,
        0x0A02AD2674FCA981, 0x9FDDD720F0A8E20F, 0x185E1CE296A32BEF, 0x75DBD8E98B72EFFD, 0x3BE06359F0499172, 0x954DB672B4AA0A23,
        0x58709DF244850981, 0x26D184B116711131, 0x72246C937CC5C02B, 0x50F539524A44357F, 0x7F2F80332507BBB3, 0x9C3D4F84E03C7B30,
        0xF9ECCA3E31E50164, 0xCF9C706CC24BBCD1, 0x42E704A21EC82AE7, 0xED4BB0A491CBCC9E, 0xDB55432429DC87F9, 0xDAE5B2CC52859E78,
        0x9E506277FD25E53A, 0x2139B8A5CC665AFB, 0x620D97D7C3BF6EED, 0x26921B2919D09C9C, 0x4C97636E0567C279, 0x6F094C634E5D3DC7,
        0x014C0043035A0212, 0xD63B8B242A91C0B9, 0xDD0935AF699F7DDC, 0x921BBBC5A7E9A523, 0xBDA46D1454F47C82, 0xB3CCE6081F92FD5A,
        0x18EC97CFB740D750, 0x1FE2614A54957019, 0x0DC4361B4C920C9D, 0x5316F51C539B9511, 0x704242DA7D4AB559, 0x852741C9D4011776,
        0xCEED315DBA85FE61, 0xDF5AD26E89C74A5A, 0x65AB333195052B5A, 0xB8A4227662141C8B, 0x2FA9012501DDDC0C, 0x3CC9FF002A1C7A92,
        0x70998F781920F765, 0xE5CFE8FF6510E321, 0x8377904C674E64A3, 0x1C3779EDC5CEF7C2, 0x0ACDC568201724E0, 0x16A48444363A03EB,
        0xE01B12FFF6C3E40E, 0x1D8616456958AEF2, 0xD86E6271EF500401, 0x3CB489DD527DADBA, 0xEEC8B6EA85028BC9, 0xA25DA0D90CCEC246,
        0xA503AA8E9470A8C7, 0x6BBB6BC489971370, 0x9B671E8B65D5B020, 0xCFC0FDBC0263100A, 0xE64C5B41ED0E4548, 0x0316F0F63124BD52,
        0xEB71A97293B34DE9, 0xCDAA79A524AADA10, 0xB77798C67BE31D94, 0xA2DA0DF6FF2AE86B, 0x8C4577E86B8036BE, 0xC31993592DC17B4C,
        0x194A6FD595CEBFD1, 0xEE7E5ABCEF9D77E4, 0xCA0C202AFDA31985, 0x72C10188BE877936, 0x692CCF63C6D5C273, 0x4DBA5093A92F84ED,
        0x48CCC6AABC2A1953, 0xE9707483CFC2F35E, 0x16DDBE48C122DEDC, 0x85E254E9B1B89B9B, 0xC03AFBD612A6EDF6, 0xB12E99AAB3F3DD87,
        0x40B44B7C6C706663, 0x1DEB70F69221A817, 0x7DFD20318BFC2B26, 0xBB376F170FDB77B4, 0x07F1E42DB6CA8E89, 0x68E6ABC024D4EB41,
        0x15EDAD0B4A5FA012, 0xE9C1F683AA9DA856, 0x5ECA84858B6DF73F, 0x797EBFB6E27F6FA2, 0x5B1DB93F2A419C20, 0x0F855BA17FE1FF41,
        0xCF8A0CD9D861860A, 0xBAAF536BF9ECDB9B, 0x63CE59E556EFCC52, 0x35E105B7CC10CB71, 0xCD5849739C326E32, 0xCC3F5B2FE8802939,
        0x1B0168375691DBC8, 0x748498A1172E5258, 0x5C38159AC054A64D, 0xD5542DF547B13C4C, 0xD7DB84F90C176A4B, 0xA170EC874D8CA869,
        0x2DC2352C7A887DC5, 0xB91A63DDFFC9E000, 0xC30B5023683353E6, 0x694834E8ACC2974B, 0xD0BE6D32F684742F, 0x9F7076E6EF45EAE0,
        0x68B2971A8205D54B, 0x954009FC051FE181, 0xF85902C5235065B7, 0xAFA1CABF76AD895A, 0xCD225EFFBCC167AF, 0xEE53DA9A2A0A9296,
        0xB113EF3E0B6616B5, 0xE571FD235343698E, 0x8817D5E92C4FC525, 0x4E2000483321B75C, 0x6DB7B27D582FC459, 0x535AC1C06B2C2334,
        0x302C92155443BEC7, 0xB0DCA54EC1A8CD50, 0x301EF701B311783E, 0x8A53B232B5907CFA, 0x37991F361926CC6F, 0xB670E5E935161DF1,
        0x78DA44F6BC0F0EAE, 0x91861197DD557D6F, 0x74B1A49B974BAB3B, 0x5103908F8721F118, 0x7A7F4A7CF5B9F29F, 0x088D645BF1780223,
        0x75FFF89A9BB1BF6C, 0x304224DD175F2CAB, 0x5AE75BB35EDC8F9A, 0x8471AA73FDF7DCCA, 0x6EB26D54402DC36C, 0xB8892E9D181F7962,
        0xB61D0B0543430620, 0x65199F858A405D9E, 0xA7EFBF7F7BD1558D, 0x9FB644F67B2E6EA2, 0xFF25F109EA0C70DB, 0xBC4DB16515AA362D,
        0x6A2D03B333CB6244, 0x8D15DBE2558B38F3, 0xA66E4835AA979AE7, 0x0A8FB317C45282FF, 0x7EFD385B4EE38B21, 0xB8A1353A6A6D3F34,
        0x7BBBF24D4B984E4B, 0xD1084E323646C2BF, 0x205A92BEF6070BE1, 0x2D14E32653B30895, 0x37154AB5B1B02586, 0x42EE1C0699255A58,
        0x1689BB948FC3C45F, 0xC46D7D3D72FF0B6F, 0x0D3BAF0D33177A18, 0x17B766E399FBCCE4, 0xAE05F266D6186F15, 0xF871A0D4440FB612,
        0x1C7777470B68462B, 0xD18B0875FCD6661E, 0xB6701527BEA193FF, 0x0195AB9E794D88A2, 0x48AB4E3724D9EABA, 0x154E09A0A6F9F2A9,
        0x03546C4CE643B5EA, 0x52015A7C2C9969E2, 0x1FE5D3220DB47E6C, 0xE48852A09EC873E6, 0x3727D01551F70E9D, 0x3850BAD9F7E77F97,
        0xF517A919DEDEAB2E, 0xA8BD9548E20AD56E, 0x90421B96618A8860, 0xD1CE79B8E27527B9, 0x503ED27A55BFF283, 0xC72296714AFEA531,
        0x7074F3F143EB96B6, 0xE1B151D890E14EE1, 0x88651E4B21D8441E, 0xD30A868B2004AFD0, 0xE409A2224F1E3931, 0x2A1EF6F9708EB13A,
        0xBD09A299FDEFE483, 0x4AE8D96C64CF42DF, 0x2F77146918F749F7, 0x785A466526A54A6A, 0x0A339A2D3B424827, 0xD132A61398E09C08,
        0xDF1F8CAE43E3BD69, 0xF9D585023C484AA7, 0x6D535F9BD446696A, 0xFE6D75B7E0987765, 0x808D85A7CEB12868, 0xA0DB7B5C9EA34E6A,
        0x6E20970C9AD6C9D1, 0xBB4D001DC034957D, 0x3F135640601C7838, 0x4FE26CA57CD92A3C, 0x6BA9D2CE3F133AAC,
    ];

    // ---- 64x64->128 primitives (dpml_private.h XMUL family), via UInt128 ----

    private static void TrigXMul(ulong a, ulong b, out ulong hi, out ulong lo)
    {
        UInt128 p = (UInt128)a * b;
        hi = p.Upper;
        lo = p.Lower;
    }

    private static void TrigXMulAdd(ulong a, ulong b, ulong addLo, out ulong hi, out ulong lo)
    {
        UInt128 p = (UInt128)a * b + addLo;
        hi = p.Upper;
        lo = p.Lower;
    }

    private static void TrigXMulXAdd(ulong a, ulong b, ulong addHi, ulong addLo, out ulong hi, out ulong lo)
    {
        UInt128 p = (UInt128)a * b + new UInt128(addHi, addLo);
        hi = p.Upper;
        lo = p.Lower;
    }

    private static void TrigXMulXAddC(ulong a, ulong b, ulong addHi, ulong addLo, out ulong carryOut, out ulong hi, out ulong lo)
    {
        UInt128 prod = (UInt128)a * b;
        UInt128 addend = new UInt128(addHi, addLo);
        UInt128 s = prod + addend;
        carryOut = (s < addend) ? 1UL : 0UL;
        hi = s.Upper;
        lo = s.Lower;
    }

    private static void TrigXMulXAddCwCarryIn(ulong a, ulong b, ulong addHi, ulong addLo, ulong carryIn, out ulong carryOut, out ulong hi, out ulong lo)
    {
        // carry_in is injected at bit 64 (no carry out possible there); carry_out is from the addend add.
        UInt128 prod = (UInt128)a * b + new UInt128(carryIn, 0);
        UInt128 addend = new UInt128(addHi, addLo);
        UInt128 s = prod + addend;
        carryOut = (s < addend) ? 1UL : 0UL;
        hi = s.Upper;
        lo = s.Lower;
    }

    // W_HAS_M_BIT_LOSS (dpml_rdx_x.h): true when the MSD is close to a multiple of pi/2.
    private static bool TrigWordHasBitLoss(ulong msd) => ((msd + 0x40000000000000UL) & 0x3F80000000000000UL) == 0;

    // UX_RADIAN_REDUCE (Payne-Hanek). Returns quadrant (0..3); reduced lies in [-pi/4, pi/4].
    private static int DiyFp128RadianReduce(scoped in DiyFp128 xIn, int octant, out DiyFp128 reduced)
    {
        DiyFp128 x = xIn;
        DiyFp128Normalize(ref x);

        // GET_F_DIGITS: F1 = high fraction limb, F0 = low.
        ulong f1 = x._hi;
        ulong f0 = x._lo;
        int exponent = x._exponent;
        uint signX = x._sign;

        if (exponent < 0)
        {
            // |x| < 0.5: quadrant follows octant parity, with an optional +/- pi/4 adjust.
            int jj = octant + (int)(signX >> 31);
            int jr = jj + (jj & 1);
            int quad = jr >> 1;
            int jd = octant - jr;

            if (jd != 0)
            {
                DiyFp128 r = default;
                DiyFp128AddSub(x, TrigPiOverFour, jd < 0 ? UxSub : UxAdd, new Span<DiyFp128>(ref r));
                reduced = r;
            }
            else
            {
                reduced = x;
            }

            return quad;
        }

        // Index the 4/pi table by the bit offset of the first interesting bit.
        int offset = exponent - (TrigUxPrecision + 2 - TrigFourOverPiZeroPadLength);
        int digitIndex = offset >> 6;
        offset &= 63;
        int tableIndex = digitIndex;

        // GET_G_DIGITS_FROM_TABLE (g3 == MSD): load g3..g0 plus the next digit, advancing past 5 words.
        ulong g3 = TrigFourOverPi[tableIndex + 0];
        ulong g2 = TrigFourOverPi[tableIndex + 1];
        ulong g1 = TrigFourOverPi[tableIndex + 2];
        ulong g0 = TrigFourOverPi[tableIndex + 3];
        ulong nextG = TrigFourOverPi[tableIndex + 4];
        tableIndex += 5;

        int rightShift = 0;
        if (offset != 0)
        {
            rightShift = 64 - offset;
            g3 = (g3 << offset) | (g2 >> rightShift);
            g2 = (g2 << offset) | (g1 >> rightShift);
            g1 = (g1 << offset) | (g0 >> rightShift);
            g0 = (g0 << offset) | (nextG >> rightShift);
        }

        // MULTIPLY_F_AND_G_DIGITS: w = F * G, keeping the top 256 bits in g3..g0.
        {
            TrigXMul(g0, f0, out ulong t1, out ulong t0);
            TrigXMulAdd(g0, f1, t1, out ulong t2, out t1);
            g0 = t0;
            TrigXMulXAddC(g1, f0, t2, t1, out ulong c, out t2, out t1);
            TrigXMulXAdd(g1, f1, c, t2, out t0, out t2);
            g1 = t1;
            TrigXMulXAdd(g2, f0, t0, t2, out t0, out t2);
            t0 = (g2 * f1) + t0;
            g2 = t2;
            t0 = (g3 * f0) + t0;
            g3 = t0;
        }

        // Add in the variable octant at bit 61.
        int octantSigned = (signX != 0) ? -octant : octant;
        unchecked
        {
            g3 += (ulong)(long)octantSigned << (64 - 3);
        }

        int scale = 0;
        ulong extraW;

        while (true)
        {
            if (!TrigWordHasBitLoss(g3))
            {
                break;
            }

            ulong nextDigit = nextG;
            nextG = TrigFourOverPi[tableIndex++];
            if (offset != 0)
            {
                nextDigit = (nextDigit << offset) | (nextG >> rightShift);
            }

            // GET_NEXT_PRODUCT(nextDigit, extraW, carry): add F*nextDigit at the low end of w.
            {
                ulong oldG0 = g0;
                TrigXMulXAddC(nextDigit, f0, oldG0, 0UL, out ulong carry, out g0, out extraW);
                ulong oldG1 = g1;
                TrigXMulXAddCwCarryIn(nextDigit, f1, oldG1, g0, carry, out carry, out g1, out g0);
                if (carry != 0)
                {
                    g2++;
                    if (g2 == 0)
                    {
                        g3++;
                    }
                }
            }

            // Terminate once fewer than L bits of leading 0's or 1's remain.
            ulong td = (g2 >> (64 - TrigNumExtraBits - 3)) | (g3 << (TrigNumExtraBits + 3));
            td ^= (ulong)((long)td >> 63);
            if (td != 0)
            {
                break;
            }

            // Compress w by one digit, preserving the 3 octant bits.
            const ulong OctantMask = 0xE000000000000000UL;
            g3 = (g3 & OctantMask) | (g2 & ~OctantMask);
            g2 = g1;
            g1 = g0;
            g0 = extraW;
            scale += 64;
        }

        // Sign-extend w and extract the quadrant.
        ulong quadrant = g3;
        g3 <<= 2;
        g3 = (ulong)((long)g3 >> 2);
        ulong msdSaved = g3;
        quadrant -= g3;

        if (g3 == (ulong)((long)g3 >> 63))
        {
            g3 = g2;
            g2 = g1;
            g1 = g0; // g0 (the LSD) is not consumed past this point, so it is not rotated in
            scale += 64;
        }

        uint sign = ((long)msdSaved < 0) ? UxSignBit : 0;
        if (sign != 0)
        {
            // NEGATE_W: two's complement of the 3-digit value g3:g2:g1.
            g3 = ~g3;
            g2 = ~g2;
            g1 = ~g1;
            g1 += 1;
            ulong carry = (g1 == 0) ? 1UL : 0UL;
            g2 += carry;
            carry = (g2 == 0) ? 1UL : 0UL;
            g3 += carry;
        }

        unchecked
        {
            quadrant = (signX != 0) ? (0UL - quadrant) : quadrant;
        }

        reduced = default;
        reduced._sign = sign ^ signX;
        reduced._exponent = 3;
        reduced._hi = g3; // PUT_W_DIGITS
        reduced._lo = g2;
        DiyFp128Normalize(ref reduced);

        int normExponent = reduced._exponent;
        int reinjectOffset = normExponent - 3;
        if (reinjectOffset != 0)
        {
            reinjectOffset += 64;
            reduced._lo |= g1 >> reinjectOffset; // reinject bits shifted out of LSD_OF_W (g1)
        }
        reduced._exponent = normExponent - scale;

        DiyFp128 piOverFour = TrigPiOverFour;
        DiyFp128Multiply(ref reduced, ref piOverFour, out reduced);

        return (int)((quadrant >> 62) & 3);
    }

    // EVALUATE_RATIONAL (dpml_ux_ops_64.c) with the numerator/denominator supplied as explicit
    // coefficient blocks plus their trailing exponent adjust. An absent half is passed as default.
    private static void DiyFp128EvaluateRational(scoped in DiyFp128 argIn,
        ReadOnlySpan<DiyFp128FixedCoefficient> numerator, int numeratorTrailingExponent,
        ReadOnlySpan<DiyFp128FixedCoefficient> denominator, int denominatorTrailingExponent,
        int degree, int flags, Span<DiyFp128> result)
    {
        DiyFp128 argument = argIn;
        int sign = flags;

        DiyFp128 polyArg;
        if ((flags & (TrigSquareTerm | (TrigSquareTerm << TrigNumeratorFieldWidth))) != 0)
        {
            DiyFp128 a = argument;
            DiyFp128Multiply(ref a, ref a, out polyArg);
        }
        else
        {
            polyArg = argument;
            int adjust = (argument._sign != 0) ? (TrigAlternateSign | (TrigAlternateSign << TrigNumeratorFieldWidth)) : 0;
            sign = flags ^ adjust;
        }

        DiyFp128Normalize(ref polyArg);
        long shift = -(long)degree * polyArg._exponent;

        int tmp = (((flags & TrigSwap) == 0) || ((flags & TrigSkip) != 0)) ? 0 : 1;
        int firstIndex = tmp;
        int secondIndex = 1 - tmp;

        bool hasNumerator = (flags & TrigNumeratorMask) != 0;
        bool hasDenominator = (flags & TrigDenominatorMask) != 0;

        if (hasNumerator)
        {
            int index = hasDenominator ? firstIndex : 0;
            DiyFp128 r;
            if ((sign & TrigAlternateSign) != 0)
            {
                DiyFp128EvaluateNegativePolynomial(polyArg, shift, numerator, 0, degree, out r);
            }
            else
            {
                DiyFp128EvaluatePositivePolynomial(polyArg, shift, numerator, 0, degree, out r);
            }
            if ((flags & TrigPostMultiply) != 0)
            {
                DiyFp128 a = argument;
                DiyFp128Multiply(ref a, ref r, out r);
            }
            r._exponent += numeratorTrailingExponent;
            result[index] = r;
        }
        else
        {
            secondIndex = 0;
            flags |= TrigNoDivide;
        }

        if (hasDenominator)
        {
            DiyFp128 r;
            if ((sign & (TrigAlternateSign << TrigNumeratorFieldWidth)) != 0)
            {
                DiyFp128EvaluateNegativePolynomial(polyArg, shift, denominator, 0, degree, out r);
            }
            else
            {
                DiyFp128EvaluatePositivePolynomial(polyArg, shift, denominator, 0, degree, out r);
            }
            if ((flags & (TrigPostMultiply << TrigNumeratorFieldWidth)) != 0)
            {
                DiyFp128 a = argument;
                DiyFp128Multiply(ref a, ref r, out r);
            }
            r._exponent += denominatorTrailingExponent;
            result[secondIndex] = r;
            if ((flags & TrigSkip) != 0)
            {
                return;
            }
        }
        else
        {
            flags |= TrigNoDivide;
        }

        if ((flags & TrigNoDivide) == 0)
        {
            DiyFp128Divide(result[0], result[1], DiyFp128FullPrecision, out result[0]);
        }
    }

    // UX_SINCOS: fills result[0] (sin/primary) and, for sincos, result[1] (cos).
    private static void DiyFp128SinCos(scoped in DiyFp128 arg, int octant, int functionCode, Span<DiyFp128> result)
    {
        int quadrant = DiyFp128RadianReduce(arg, octant, out DiyFp128 reduced);

        if (functionCode == TrigSinCosFunc)
        {
            int flags = TrigSinPolyFlags | TrigCosPolyFlags | TrigNoDivide;
            if ((quadrant & 1) != 0)
            {
                flags |= TrigSwap;
            }
            DiyFp128EvaluateRational(reduced, TrigSinCoefficients, 1, TrigCosCoefficients, 1, TrigSinCosDegree, flags, result);
        }
        else if ((quadrant & 1) != 0)
        {
            DiyFp128EvaluateRational(reduced, default, 0, TrigCosCoefficients, 1, TrigSinCosDegree, TrigSkip | TrigCosPolyFlags, result);
        }
        else
        {
            DiyFp128EvaluateRational(reduced, TrigSinCoefficients, 1, default, 0, TrigSinCosDegree, TrigSkip | TrigSinPolyFlags, result);
        }

        if ((quadrant & 2) != 0)
        {
            result[0]._sign ^= UxSignBit;
        }
        if ((functionCode == TrigSinCosFunc) && (((quadrant + 1) & 2) != 0))
        {
            result[1]._sign ^= UxSignBit;
        }
    }

    private static DiyFp128 DiyFp128Sin(scoped in DiyFp128 arg)
    {
        Span<DiyFp128> r = [default, default];
        DiyFp128SinCos(arg, 0, 1, r);
        return r[0];
    }

    private static DiyFp128 DiyFp128Cos(scoped in DiyFp128 arg)
    {
        Span<DiyFp128> r = [default, default];
        DiyFp128SinCos(arg, 2, 2, r);
        return r[0];
    }

    private static void DiyFp128SinCosPair(scoped in DiyFp128 arg, out DiyFp128 sin, out DiyFp128 cos)
    {
        Span<DiyFp128> r = [default, default];
        DiyFp128SinCos(arg, 0, TrigSinCosFunc, r);
        sin = r[0];
        cos = r[1];
    }

    private static DiyFp128 DiyFp128Tan(scoped in DiyFp128 arg)
    {
        int quadrant = DiyFp128RadianReduce(arg, 0, out DiyFp128 reduced);

        if ((reduced._hi | reduced._lo) == 0)
        {
            // Reduced argument is exactly zero (x == 0): tan == 0.
            return reduced;
        }

        int divideFlag = ((quadrant & 1) != 0) ? TrigSwap : 0;
        int flags = (TrigSquareTerm | TrigAlternateSign | TrigPostMultiply)
                  | ((TrigSquareTerm | TrigAlternateSign) << TrigNumeratorFieldWidth)
                  | divideFlag;

        Span<DiyFp128> r = [default, default];
        DiyFp128EvaluateRational(reduced, TrigTanNumeratorCoefficients, 1, TrigTanDenominatorCoefficients, 1, TrigTanCotDegree, flags, r);

        if ((quadrant & 1) != 0)
        {
            r[0]._sign ^= UxSignBit;
        }
        return r[0];
    }
}
