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

    private const int TrigSinCosDegree = 0xd;
    private const int TrigTanCotDegree = 0x7;

    private const int TrigSinCosFunc = 3;

    // dpml_ux.h reduction constants: UX_PRECISION, FOUR_OV_PI_ZERO_PAD_LEN, NUM_EXTRA_BITS.
    private const int TrigUxPrecision = 128;
    private const int TrigFourOverPiZeroPadLength = 138;
    private const int TrigNumExtraBits = 6;

    // Unpacked pi/4 (dpml_trig_x.h) and pi (pi/4 with binary exponent + 2).
    private static DiyFp128 TrigPiOverFour => new DiyFp128(0, 0, 0xc90fdaa22168c234, 0xc4c6628b80dc1cd1);

    private static readonly DiyFp128FixedCoefficient[] TrigSinCoefficients =
    [
        new(0x000000039e634562, 0x0000000000000000),
        new(0x000009f9dce17a1d, 0x0000000000000000),
        new(0x001761b4083a3075, 0x0000000000000000),
        new(0x2e371dedb1d75408, 0x0000000000000000),
        new(0xd26d1a05013c755b, 0x000000000000004b),
        new(0x1dc0c2b528320429, 0x000000000000654b),
        new(0x9ccee07c4701facd, 0x00000000006b9fcf),
        new(0xa1b425f28dfbf381, 0x000000005849184e),
        new(0x89c71fce8fc76db3, 0x00000035cc8acfea),
        new(0x338faac1c88e2826, 0x0000171de3a556c7),
        new(0x8068068068067e8f, 0x0006806806806806),
        new(0x1111111111111106, 0x0111111111111111),
        new(0x5555555555555555, 0x1555555555555555),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    private static readonly DiyFp128FixedCoefficient[] TrigCosCoefficients =
    [
        new(0x00000061a9fb87e2, 0x0000000000000000),
        new(0x0000f96669688c7c, 0x0000000000000000),
        new(0x0219c72c77c1de0c, 0x0000000000000000),
        new(0xca85747f51903a09, 0x0000000000000003),
        new(0x9e18ee5eeb393833, 0x00000000000005a0),
        new(0xf9ccee079837094c, 0x000000000006b9fc),
        new(0x301f274823772903, 0x00000000064e5d2a),
        new(0x3625ed5134a72fa4, 0x000000047bb63bfe),
        new(0xeb8e5de02d6a41e7, 0x0000024fc9f6ef13),
        new(0xd00d00d00cfbffe1, 0x0000d00d00d00d00),
        new(0x82d82d82d82d4910, 0x002d82d82d82d82d),
        new(0x55555555555553eb, 0x0555555555555555),
        new(0xfffffffffffffffc, 0x3fffffffffffffff),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    private static readonly DiyFp128FixedCoefficient[] TrigTanNumeratorCoefficients =
    [
        new(0x0000000000000000, 0x0000000000000000),
        new(0x02e36384ab86d966, 0x00000000004583dc),
        new(0xfe661c77c57437cf, 0x00000001df2fb0d7),
        new(0xe5f9c2190062ee42, 0x0000036f46cae26e),
        new(0x9c878717e15a162f, 0x000269dca6fa2240),
        new(0xc5b462ffe65127b0, 0x00b209c04c0b8a2c),
        new(0xa273c25867f59a68, 0x12f6c9c3d7a587c9),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    private static readonly DiyFp128FixedCoefficient[] TrigTanDenominatorCoefficients =
    [
        new(0x196c967acbfc02d6, 0x000000000000a9ba),
        new(0xd03d3831cf5f2fe6, 0x000000000e1ae92d),
        new(0x40a36a10fd241f97, 0x0000002e4c98a51d),
        new(0xb12a527e72e00402, 0x0000338085b96b4f),
        new(0xf5e92d642c15cbc5, 0x001739c356378673),
        new(0x7902cb9a86202dfc, 0x042c1f7ebbbfdf42),
        new(0x4d1e6d0312a04513, 0x3da1746e82503274),
        new(0x0000000000000000, 0x8000000000000000),
    ];

    // dpml_four_over_pi.c: the leading 263 x 64-bit digits of 4/pi (with two words of zero padding).
    private static ReadOnlySpan<ulong> TrigFourOverPi =>
    [
        0x0000000000000000, 0x0000000000000000, 0x0028be60db939105, 0x4a7f09d5f47d4d37, 0x7036d8a5664f10e4, 0x107f9458eaf7aef1,
        0x586dc91b8e909374, 0xb801924bba827464, 0x873f877ac72c4a69, 0xcfba208d7d4baed1, 0x213a671c09ad17df, 0x904e64758e60d4ce,
        0x7d272117e2ef7e4a, 0x0ec7fe25fff78166, 0x03fbcbc462d6829b, 0x47db4d9fb3c9f2c2, 0x6dd3d18fd9a797fa, 0x8b5d49eeb1faf97c,
        0x5ecf41ce7de294a4, 0xba9afed7ec47e357, 0x421580cc11bf1eda, 0xeafc33ef0826bd0d, 0x876a78e45857b986, 0xc219666157c5281a,
        0x10237ff620135cc9, 0xcc41818555b29cea, 0x3258389ef0231ad1, 0xf10670d9f3773a02, 0x4aa0d6711da2e587, 0x29b76bd13455c641,
        0x4fa97fc1c14fdf8c, 0xfa0cb0b793e60c9f, 0x6ef0cf49bbdac797, 0xbe27ce87cd72bc9f, 0xc761fc48641f1f09, 0x1abe9bb55dcb4c10,
        0xcec571852d674670, 0xf0b12b50534b1740, 0x03119f618b5c78e6, 0xb1a6c0188cdf34ad, 0x25e9ed35554dfd8f, 0xb5c60428ff1d934a,
        0xa7592af5dc3e1f18, 0xd5ec1eb9c545d592, 0x7036758ece2129f2, 0xc8c91de2b588d516, 0xae47c006c2bc77f3, 0x867fcc67da879998,
        0x55e651feeb361fdf, 0xadd948a27a0c982f, 0xf9b3713bc24d9b35, 0x0fd775f785b78ed6, 0x24a6f78a08b4ba21, 0x8a1356388cb2b185,
        0xb8c232df78143005, 0xe9c77cd6f8060d04, 0xcb9884a0c05220d6, 0xe3bd5fec2b7cba47, 0x90d29234d9c43637, 0x6a9097ebb3985aa9,
        0x0a02ad2674fca981, 0x9fddd720f0a8e20f, 0x185e1ce296a32bef, 0x75dbd8e98b72effd, 0x3be06359f0499172, 0x954db672b4aa0a23,
        0x58709df244850981, 0x26d184b116711131, 0x72246c937cc5c02b, 0x50f539524a44357f, 0x7f2f80332507bbb3, 0x9c3d4f84e03c7b30,
        0xf9ecca3e31e50164, 0xcf9c706cc24bbcd1, 0x42e704a21ec82ae7, 0xed4bb0a491cbcc9e, 0xdb55432429dc87f9, 0xdae5b2cc52859e78,
        0x9e506277fd25e53a, 0x2139b8a5cc665afb, 0x620d97d7c3bf6eed, 0x26921b2919d09c9c, 0x4c97636e0567c279, 0x6f094c634e5d3dc7,
        0x014c0043035a0212, 0xd63b8b242a91c0b9, 0xdd0935af699f7ddc, 0x921bbbc5a7e9a523, 0xbda46d1454f47c82, 0xb3cce6081f92fd5a,
        0x18ec97cfb740d750, 0x1fe2614a54957019, 0x0dc4361b4c920c9d, 0x5316f51c539b9511, 0x704242da7d4ab559, 0x852741c9d4011776,
        0xceed315dba85fe61, 0xdf5ad26e89c74a5a, 0x65ab333195052b5a, 0xb8a4227662141c8b, 0x2fa9012501dddc0c, 0x3cc9ff002a1c7a92,
        0x70998f781920f765, 0xe5cfe8ff6510e321, 0x8377904c674e64a3, 0x1c3779edc5cef7c2, 0x0acdc568201724e0, 0x16a48444363a03eb,
        0xe01b12fff6c3e40e, 0x1d8616456958aef2, 0xd86e6271ef500401, 0x3cb489dd527dadba, 0xeec8b6ea85028bc9, 0xa25da0d90ccec246,
        0xa503aa8e9470a8c7, 0x6bbb6bc489971370, 0x9b671e8b65d5b020, 0xcfc0fdbc0263100a, 0xe64c5b41ed0e4548, 0x0316f0f63124bd52,
        0xeb71a97293b34de9, 0xcdaa79a524aada10, 0xb77798c67be31d94, 0xa2da0df6ff2ae86b, 0x8c4577e86b8036be, 0xc31993592dc17b4c,
        0x194a6fd595cebfd1, 0xee7e5abcef9d77e4, 0xca0c202afda31985, 0x72c10188be877936, 0x692ccf63c6d5c273, 0x4dba5093a92f84ed,
        0x48ccc6aabc2a1953, 0xe9707483cfc2f35e, 0x16ddbe48c122dedc, 0x85e254e9b1b89b9b, 0xc03afbd612a6edf6, 0xb12e99aab3f3dd87,
        0x40b44b7c6c706663, 0x1deb70f69221a817, 0x7dfd20318bfc2b26, 0xbb376f170fdb77b4, 0x07f1e42db6ca8e89, 0x68e6abc024d4eb41,
        0x15edad0b4a5fa012, 0xe9c1f683aa9da856, 0x5eca84858b6df73f, 0x797ebfb6e27f6fa2, 0x5b1db93f2a419c20, 0x0f855ba17fe1ff41,
        0xcf8a0cd9d861860a, 0xbaaf536bf9ecdb9b, 0x63ce59e556efcc52, 0x35e105b7cc10cb71, 0xcd5849739c326e32, 0xcc3f5b2fe8802939,
        0x1b0168375691dbc8, 0x748498a1172e5258, 0x5c38159ac054a64d, 0xd5542df547b13c4c, 0xd7db84f90c176a4b, 0xa170ec874d8ca869,
        0x2dc2352c7a887dc5, 0xb91a63ddffc9e000, 0xc30b5023683353e6, 0x694834e8acc2974b, 0xd0be6d32f684742f, 0x9f7076e6ef45eae0,
        0x68b2971a8205d54b, 0x954009fc051fe181, 0xf85902c5235065b7, 0xafa1cabf76ad895a, 0xcd225effbcc167af, 0xee53da9a2a0a9296,
        0xb113ef3e0b6616b5, 0xe571fd235343698e, 0x8817d5e92c4fc525, 0x4e2000483321b75c, 0x6db7b27d582fc459, 0x535ac1c06b2c2334,
        0x302c92155443bec7, 0xb0dca54ec1a8cd50, 0x301ef701b311783e, 0x8a53b232b5907cfa, 0x37991f361926cc6f, 0xb670e5e935161df1,
        0x78da44f6bc0f0eae, 0x91861197dd557d6f, 0x74b1a49b974bab3b, 0x5103908f8721f118, 0x7a7f4a7cf5b9f29f, 0x088d645bf1780223,
        0x75fff89a9bb1bf6c, 0x304224dd175f2cab, 0x5ae75bb35edc8f9a, 0x8471aa73fdf7dcca, 0x6eb26d54402dc36c, 0xb8892e9d181f7962,
        0xb61d0b0543430620, 0x65199f858a405d9e, 0xa7efbf7f7bd1558d, 0x9fb644f67b2e6ea2, 0xff25f109ea0c70db, 0xbc4db16515aa362d,
        0x6a2d03b333cb6244, 0x8d15dbe2558b38f3, 0xa66e4835aa979ae7, 0x0a8fb317c45282ff, 0x7efd385b4ee38b21, 0xb8a1353a6a6d3f34,
        0x7bbbf24d4b984e4b, 0xd1084e323646c2bf, 0x205a92bef6070be1, 0x2d14e32653b30895, 0x37154ab5b1b02586, 0x42ee1c0699255a58,
        0x1689bb948fc3c45f, 0xc46d7d3d72ff0b6f, 0x0d3baf0d33177a18, 0x17b766e399fbcce4, 0xae05f266d6186f15, 0xf871a0d4440fb612,
        0x1c7777470b68462b, 0xd18b0875fcd6661e, 0xb6701527bea193ff, 0x0195ab9e794d88a2, 0x48ab4e3724d9eaba, 0x154e09a0a6f9f2a9,
        0x03546c4ce643b5ea, 0x52015a7c2c9969e2, 0x1fe5d3220db47e6c, 0xe48852a09ec873e6, 0x3727d01551f70e9d, 0x3850bad9f7e77f97,
        0xf517a919dedeab2e, 0xa8bd9548e20ad56e, 0x90421b96618a8860, 0xd1ce79b8e27527b9, 0x503ed27a55bff283, 0xc72296714afea531,
        0x7074f3f143eb96b6, 0xe1b151d890e14ee1, 0x88651e4b21d8441e, 0xd30a868b2004afd0, 0xe409a2224f1e3931, 0x2a1ef6f9708eb13a,
        0xbd09a299fdefe483, 0x4ae8d96c64cf42df, 0x2f77146918f749f7, 0x785a466526a54a6a, 0x0a339a2d3b424827, 0xd132a61398e09c08,
        0xdf1f8cae43e3bd69, 0xf9d585023c484aa7, 0x6d535f9bd446696a, 0xfe6d75b7e0987765, 0x808d85a7ceb12868, 0xa0db7b5c9ea34e6a,
        0x6e20970c9ad6c9d1, 0xbb4d001dc034957d, 0x3f135640601c7838, 0x4fe26ca57cd92a3c, 0x6ba9d2ce3f133aac,
    ];

    // ---- 64x64->128 primitives (dpml_private.h XMUL family), via UInt128 ----

    private static void TrigXMul(ulong a, ulong b, out ulong hi, out ulong lo)
    {
        UInt128 p = (UInt128)a * b;
        hi = (ulong)(p >> 64);
        lo = (ulong)p;
    }

    private static void TrigXMulAdd(ulong a, ulong b, ulong addLo, out ulong hi, out ulong lo)
    {
        UInt128 p = (UInt128)a * b + addLo;
        hi = (ulong)(p >> 64);
        lo = (ulong)p;
    }

    private static void TrigXMulXAdd(ulong a, ulong b, ulong addHi, ulong addLo, out ulong hi, out ulong lo)
    {
        UInt128 p = (UInt128)a * b + (((UInt128)addHi << 64) | addLo);
        hi = (ulong)(p >> 64);
        lo = (ulong)p;
    }

    private static void TrigXMulXAddC(ulong a, ulong b, ulong addHi, ulong addLo, out ulong carryOut, out ulong hi, out ulong lo)
    {
        UInt128 prod = (UInt128)a * b;
        UInt128 addend = ((UInt128)addHi << 64) | addLo;
        UInt128 s = prod + addend;
        carryOut = (s < addend) ? 1UL : 0UL;
        hi = (ulong)(s >> 64);
        lo = (ulong)s;
    }

    private static void TrigXMulXAddCwCarryIn(ulong a, ulong b, ulong addHi, ulong addLo, ulong carryIn, out ulong carryOut, out ulong hi, out ulong lo)
    {
        // carry_in is injected at bit 64 (no carry out possible there); carry_out is from the addend add.
        UInt128 prod = (UInt128)a * b + ((UInt128)carryIn << 64);
        UInt128 addend = ((UInt128)addHi << 64) | addLo;
        UInt128 s = prod + addend;
        carryOut = (s < addend) ? 1UL : 0UL;
        hi = (ulong)(s >> 64);
        lo = (ulong)s;
    }

    // W_HAS_M_BIT_LOSS (dpml_rdx_x.h): true when the MSD is close to a multiple of pi/2.
    private static bool TrigWordHasBitLoss(ulong msd) => ((msd + 0x40000000000000UL) & 0x3f80000000000000UL) == 0;

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
                Span<DiyFp128> r = stackalloc DiyFp128[1];
                DiyFp128AddSub(x, TrigPiOverFour, jd < 0 ? UxSub : UxAdd, r);
                reduced = r[0];
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
        ulong extraW = 0;

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
            extraW = 0;
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
            g1 = g0;
            g0 = extraW;
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
        Span<DiyFp128> r = stackalloc DiyFp128[2];
        DiyFp128SinCos(arg, 0, 1, r);
        return r[0];
    }

    private static DiyFp128 DiyFp128Cos(scoped in DiyFp128 arg)
    {
        Span<DiyFp128> r = stackalloc DiyFp128[2];
        DiyFp128SinCos(arg, 2, 2, r);
        return r[0];
    }

    private static void DiyFp128SinCosPair(scoped in DiyFp128 arg, out DiyFp128 sin, out DiyFp128 cos)
    {
        Span<DiyFp128> r = stackalloc DiyFp128[2];
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

        Span<DiyFp128> r = stackalloc DiyFp128[2];
        DiyFp128EvaluateRational(reduced, TrigTanNumeratorCoefficients, 1, TrigTanDenominatorCoefficients, 1, TrigTanCotDegree, flags, r);

        if ((quadrant & 1) != 0)
        {
            r[0]._sign ^= UxSignBit;
        }
        return r[0];
    }
}
