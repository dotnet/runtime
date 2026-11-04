// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        // The entries descend by powers-of-two exponents; extraction greedily builds the valuation.
        internal static ReadOnlySpan<uint> Pow3FactorizationPowers =>
        [
            43_046_721, 6_561, 81, 9, 3,
        ];

        internal static ReadOnlySpan<uint> Pow5FactorizationPowers =>
        [
            390_625, 625, 25, 5,
        ];

        internal static ReadOnlySpan<uint> Pow7FactorizationPowers =>
        [
            5_764_801, 2_401, 49, 7,
        ];

        internal static int ExtractFactorPower(ref nuint value, nuint factor, ReadOnlySpan<uint> factorizationPowers)
        {
            Debug.Assert(value % factor == 0);

            value /= factor;
            int exponent = 1;

            for (int i = 0; i < factorizationPowers.Length; i++)
            {
                if (value == 1)
                {
                    break;
                }

                nuint factorPower = factorizationPowers[i];

                int factorExponent = 1 << (factorizationPowers.Length - i - 1);
                while (value % factorPower == 0)
                {
                    value /= factorPower;
                    exponent += factorExponent;
                }
            }

            return exponent;
        }

        /// <summary>
        /// Computes bounds for the bit length of
        /// (<c>2^shift * 3^powerOfThree * 5^powerOfFive * 7^powerOfSeven</c>) raised to <paramref name="exponent"/>.
        /// </summary>
        /// <remarks>
        /// The lower bound supports overflow checks before allocation; the upper bound sizes the
        /// destination without constructing the power.
        /// </remarks>
        /// <returns>The lower and upper bit-length bounds.</returns>
        internal static (ulong LowerBound, ulong UpperBound) GetPowBitLengthBounds(
            int powerOfThree,
            int powerOfFive,
            int powerOfSeven,
            int shift,
            int exponent)
        {
            // A UInt128 represents an unsigned binary fixed-point value x as
            // floor(x * 2^64). Its range is [0, 2^64) in increments of 2^-64:
            // the upper 64 bits hold the integer part and the lower 64 bits hold
            // the binary fraction.
            // Accordingly, these constants are:
            //   floor((log2(3) - 1) * 2^64)
            //   floor((log2(5) - 2) * 2^64)
            // Prepending 1 and 2 below reconstructs floor(log2(3) * 2^64) and
            // floor(log2(5) * 2^64), respectively.
            const ulong Log2OfThreeFractionalBits = 0x95C0_1A39_FBD6_879F;
            const ulong Log2OfFiveFractionalBits = 0x5269_E12F_346E_2BF9;
            const ulong Log2OfSevenFractionalBits = 0xCEAE_CFEA_8085_9B33;

            Debug.Assert(powerOfThree >= 0);
            Debug.Assert(powerOfFive >= 0);
            Debug.Assert(powerOfSeven >= 0);
            Debug.Assert(powerOfThree != 0 || powerOfFive != 0 || powerOfSeven != 0);

            UInt128 log2OfThreeLower = ((UInt128)1 << 64) | Log2OfThreeFractionalBits;
            UInt128 log2OfFiveLower = ((UInt128)2 << 64) | Log2OfFiveFractionalBits;
            UInt128 log2OfSevenLower = ((UInt128)2 << 64) | Log2OfSevenFractionalBits;

            // log2(2^shift * 3^powerOfThree * 5^powerOfFive * 7^powerOfSeven) is the sum below.
            // The powers of two are exact; each truncated irrational logarithm is
            // less than its actual value by strictly less than one fractional unit.
            UInt128 log2Lower = ((UInt128)(uint)powerOfThree * log2OfThreeLower)
                + ((UInt128)(uint)powerOfFive * log2OfFiveLower)
                + ((UInt128)(uint)powerOfSeven * log2OfSevenLower)
                + ((UInt128)(uint)shift << 64);
            UInt128 log2Upper = log2Lower + (uint)(powerOfThree + powerOfFive + powerOfSeven);

            UInt128 exponentValue = (uint)exponent;
            // Multiply before discarding the fractional bits so the exponent does not
            // amplify an intermediate rounding. For a positive integer x, its bit
            // length is floor(log2(x)) + 1.
            ulong lower = checked((ulong)((exponentValue * log2Lower) >> 64) + 1);
            ulong upper = checked((ulong)((exponentValue * log2Upper) >> 64) + 1);
            return (lower, upper);
        }

        // The tables store b^(2^k) as length-prefixed, least-significant-limb-first magnitudes.
        // Callers form b^e by multiplying entries for the set bits of e. The first entry follows
        // the largest low-bit range that fits in one nuint: b^(2^k - 1) fits, but including the
        // next exponent bit does not, so callers combine those low bits into one scalar factor.
        // Each subsequent entry is the square of the previous one, ending at b^1024. This covers
        // every e < 2048; binary decomposition would require at most ten multiplies, reduced to
        // at most eight here by the scalar factor. Larger exponents are increasingly uncommon, but
        // the tables can be extended by appending successive squares.
        private static ReadOnlySpan<int> Pow3TableIndices64 =>
        [
            0, 2, 5, 10, 18, 32,
        ];

        private static ReadOnlySpan<ulong> Pow3TableStorage64 =>
        [
            1, 0x0006954FE21E3E81, // 3^32

            2, // 3^64
            0x7932278C797EBD01,
            0x0000002B56D4AF8F,

            4, // 3^128
            0x804214818A867A01,
            0xE4588F7CFB7BE364,
            0x48E690DA5A4D2EEF,
            0x0000000000000756,

            7, // 3^256
            0xD2105F2F0730F401,
            0x75A205DE07597D51,
            0xFED242815E55BC83,
            0xC7ADEEB80D4FFF81,
            0x4AF58B4C596F8DDC,
            0xAB97C4C85188B496,
            0x000000000035D511,

            13, // 3^512
            0x1D660D1276F1E801,
            0x7679EF1B4306890C,
            0x35BD075F4D13EBBD,
            0x9483760FBD2A7E53,
            0xF9C87FE3AA65AC28,
            0x5C83098EC0C732E0,
            0x1FE77793417DD770,
            0x90C4F81328052D31,
            0xE33FCB0F1A5ABFCA,
            0x34983E22B2A5CADD,
            0xE18F23CF2B52BBAB,
            0x459357C06F65487C,
            0x00000B51EAA7759A,

            26, // 3^1024
            0x9DA4A09B9023D001,
            0x493A45AC115F4AD6,
            0x700E71B9F9A5D9AF,
            0x009CA50AD62B958E,
            0x324D0C216329E982,
            0x16E166E1198C2C62,
            0x07E91A5815AB0CE4,
            0x455C2FA05A7850B0,
            0x9F0F45BEBEA44DEA,
            0x33665C35D9690E9A,
            0x0E53765696101393,
            0x462977C3E968F00F,
            0xA8D59A992CA486E3,
            0x6D4E09070553B130,
            0x9DF6BAF79321C28F,
            0x6F32B72085A30BEB,
            0x59970298FE3C15C4,
            0x9FD8ACDC6F63D099,
            0xC53A0D65AC3F91CE,
            0xCB25C90F3A9F084F,
            0x02F3AF1B8070E800,
            0xFB62811EC36CE372,
            0xE1987DFCF344FDC2,
            0xF16D5486242EAD38,
            0xB92A3E235D469491,
            0x0000000000802460,
        ];

        private static ReadOnlySpan<int> Pow5TableIndices64 =>
        [
            0, 2, 5, 9, 15, 26, 46,
        ];

        private static ReadOnlySpan<ulong> Pow5TableStorage64 =>
        [
            1, 0x0000002386F26FC1, // 5^16

            2, // 5^32
            0x2D6D415B85ACEF81,
            0x00000000000004EE,

            3, // 5^64
            0x6E38ED64BF6A1F01,
            0xE93FF9F4DAA797ED,
            0x0000000000184F03,

            5, // 5^128
            0x03DF99092E953E01,
            0x2374E42F0F1538FD,
            0xC404DC08D3CFF5EC,
            0xA6337F19BCCDB0DA,
            0x0000024EE91F2603,

            10, // 5^256
            0xBED3875B982E7C01,
            0x12152F87D8D99F72,
            0xCF4A6E706BDE50C6,
            0x26B2716ED595D80F,
            0x1D153624ADC666B0,
            0x63FF540E3C42D35A,
            0x65F9EF17CC5573C0,
            0x80DCC7F755BC28F2,
            0x5FDCEFCEF46EEDDC,
            0x00000000000553F7,

            19, // 5^512
            0x77F27267FC6CF801,
            0x5D96976F8F9546DC,
            0xC31E1AD9B83A8A97,
            0x94E6574746C40513,
            0x4475B579C88976C1,
            0xAA1DA1BF28F8733B,
            0x1E25CFEA703ED321,
            0xBC51FB2EB21A2F22,
            0xBFA3EDAC96E14F5D,
            0xE7FC7153329C57AE,
            0x85A91924C3FC0695,
            0xB2908EE0F95F635E,
            0x1366732A93ABADE4,
            0x69BE5B0E9449775C,
            0xB099BC817343AFAC,
            0xA269974845A71D46,
            0x8A0B1F138CB07303,
            0xC1D238D98CAB8A97,
            0x0000001C633415D4,

            38, // 5^1024
            0xF55B2B722919F001,
            0x1EC29F866E7C215B,
            0x15C51A88991C4E87,
            0x4C7D1E1A140AC535,
            0x0ED1440ECC2CD819,
            0x7DE16CFB896634EE,
            0x9FCE837D1E43F61F,
            0x233E55C7231D2B9C,
            0xF451218B65DC60D7,
            0xC96359861C5CD134,
            0xA7E89431922BBB9F,
            0x62BE695A9F9F2A07,
            0x045B7A748E1042C4,
            0x8AD822A51ABE1DE3,
            0xD814B505BA34C411,
            0x8FC51A16BF3FDEB3,
            0xF56DEEECB1B896BC,
            0xB6F4654B31FB6BFD,
            0x6B7595FB101A3616,
            0x80D98089DC1A47FE,
            0x9A20288280BDA5A5,
            0xFC8F1F9031EB0F66,
            0xE26A7B7E976A3310,
            0x3CE3A0B8DF68368A,
            0x75A351A28E4262CE,
            0x445975836CB0B6C9,
            0xC356E38A31B5653F,
            0x0190FBA035FAABA6,
            0x88BC491B9FC4ED52,
            0x005B80411640114A,
            0x1E8D4649F4F3235E,
            0x73C5534936A8DE06,
            0xC1A6970CA7E6BD2A,
            0xD2DB49EF47187094,
            0xAE6209D4926C3F5B,
            0x34F4A3C62D433949,
            0xD9D61A05D4305D94,
            0x0000000000000325,
        ];

        private static ReadOnlySpan<int> Pow7TableIndices64 =>
        [
            0, 2, 5, 9, 16, 29, 53,
        ];

        private static ReadOnlySpan<ulong> Pow7TableStorage64 =>
        [
            1, 0x00001E39A5057D81, // 7^16

            2, // 7^32
            0x303C33586E913B01,
            0x0000000003918FA8,

            3, // 7^64
            0x199417C8C0BB7601,
            0xD63B78E780E1341E,
            0x000CBC21FE4561C8,

            6, // 7^128
            0x085E49D71BDAEC01,
            0x5BF188FE799B3618,
            0xC8A2B5C32649D046,
            0xF3CC3D603192BAF6,
            0x9CE406DA4F5895F9,
            0x000000A22D71C87A,

            12, // 7^256
            0x0814E4AD0145D801,
            0x0CFAAF9C10D53258,
            0x6971852FE29D44BD,
            0x402F567BB6851623,
            0x800266CD430C356C,
            0xBDDC35A540BD987C,
            0x7BD35A98C1949DB2,
            0x94AE92E25086ABE3,
            0x6578E5B154F8EAC1,
            0x6CEF58CB25627743,
            0x8C12EE3C44FF27E1,
            0x00000000000066BD,

            23, // 7^512
            0xF151581828CBB001,
            0xE46393E111701448,
            0x1C3797F8C6A24D82,
            0x05631AA812F6E4E8,
            0xA2673CAB3D9CF244,
            0xD8519FF9D64A7DEA,
            0xEC5AA0BDFAE3E411,
            0xBBFFCDEE856FCE3F,
            0x124D6232F189DC38,
            0xBBFD810713036414,
            0xDB636E5FBC240CAF,
            0xAB40AD83F69E4EC1,
            0xA09E224A06A732FA,
            0x5D3CED80008E3FB2,
            0xC1A76E7D0208291E,
            0xE464A71E14E86615,
            0x3889E7D576D1E82E,
            0x2221C72BA96476D4,
            0x9DD4ED7BFB552E43,
            0xAC9FC6BFDF7E38DA,
            0x975E7C3EAC5ABD17,
            0x367081F17E2D6BA4,
            0x00000000293B97F7,

            45, // 7^1024
            0xF853F940EA976001,
            0x2C80DD09435D192A,
            0x0DC1F2047D01C2BF,
            0xB74658A80FA529F6,
            0xFA444EBFFCD87A0C,
            0xE467C82045A0736D,
            0xECE0249825DD9342,
            0xF187913CA08CC39A,
            0x0D5BA7F00D5C07C2,
            0x34B6FF306D315D0A,
            0x96D4F3E4F811E85F,
            0xDE90FD65694DA8FA,
            0xD932761C82BB48DE,
            0x49C9470F958B1FE5,
            0x11F5633731476EDA,
            0x3C650D4450C95C23,
            0xEFB5A1DF0C044032,
            0xA16039485E7EAE92,
            0x719082CC71496504,
            0x4E1C077763E1CD52,
            0x61A47EABD0130B33,
            0xCDF7A64BF03B33B2,
            0xC6F08D5A8DEBD3C8,
            0x3A85B19010A3209C,
            0x8B6C2A33FDAEABBB,
            0xBE53E6C024F484F3,
            0x4CDC9E263D125C65,
            0xB9E820A4DFCA20EF,
            0x5025AD27BFF8AFDA,
            0x069BF53B443BD345,
            0x61C13DF9984BE5D1,
            0x0F96C2C20FDCE741,
            0xB51672B39A6A3981,
            0xF196580AF9CF7556,
            0xCC813D93BD72C9ED,
            0x89C81F855A07729A,
            0xE76ABACB19113A71,
            0xFF8667E24C86F713,
            0x58E8322111C256E3,
            0x43C26500B11D0C06,
            0x0808AAC13DD2DC55,
            0xB7E1C05B2D7F5FAB,
            0x1D0FCC36CB2AF962,
            0x11B400893D7AC1A7,
            0x06A4248C9598B26E,
        ];

        private static ReadOnlySpan<int> Pow3TableIndices32 =>
        [
            0, 2, 5, 10, 18, 32, 59,
        ];

        private static ReadOnlySpan<uint> Pow3TableStorage32 =>
        [
            1, 0x0290D741, // 3^16

            2, // 3^32
            0xE21E3E81,
            0x0006954F,

            4, // 3^64
            0x797EBD01,
            0x7932278C,
            0x56D4AF8F,
            0x0000002B,

            7, // 3^128
            0x8A867A01,
            0x80421481,
            0xFB7BE364,
            0xE4588F7C,
            0x5A4D2EEF,
            0x48E690DA,
            0x00000756,

            13, // 3^256
            0x0730F401,
            0xD2105F2F,
            0x07597D51,
            0x75A205DE,
            0x5E55BC83,
            0xFED24281,
            0x0D4FFF81,
            0xC7ADEEB8,
            0x596F8DDC,
            0x4AF58B4C,
            0x5188B496,
            0xAB97C4C8,
            0x0035D511,

            26, // 3^512
            0x76F1E801,
            0x1D660D12,
            0x4306890C,
            0x7679EF1B,
            0x4D13EBBD,
            0x35BD075F,
            0xBD2A7E53,
            0x9483760F,
            0xAA65AC28,
            0xF9C87FE3,
            0xC0C732E0,
            0x5C83098E,
            0x417DD770,
            0x1FE77793,
            0x28052D31,
            0x90C4F813,
            0x1A5ABFCA,
            0xE33FCB0F,
            0xB2A5CADD,
            0x34983E22,
            0x2B52BBAB,
            0xE18F23CF,
            0x6F65487C,
            0x459357C0,
            0xEAA7759A,
            0x00000B51,

            51, // 3^1024
            0x9023D001,
            0x9DA4A09B,
            0x115F4AD6,
            0x493A45AC,
            0xF9A5D9AF,
            0x700E71B9,
            0xD62B958E,
            0x009CA50A,
            0x6329E982,
            0x324D0C21,
            0x198C2C62,
            0x16E166E1,
            0x15AB0CE4,
            0x07E91A58,
            0x5A7850B0,
            0x455C2FA0,
            0xBEA44DEA,
            0x9F0F45BE,
            0xD9690E9A,
            0x33665C35,
            0x96101393,
            0x0E537656,
            0xE968F00F,
            0x462977C3,
            0x2CA486E3,
            0xA8D59A99,
            0x0553B130,
            0x6D4E0907,
            0x9321C28F,
            0x9DF6BAF7,
            0x85A30BEB,
            0x6F32B720,
            0xFE3C15C4,
            0x59970298,
            0x6F63D099,
            0x9FD8ACDC,
            0xAC3F91CE,
            0xC53A0D65,
            0x3A9F084F,
            0xCB25C90F,
            0x8070E800,
            0x02F3AF1B,
            0xC36CE372,
            0xFB62811E,
            0xF344FDC2,
            0xE1987DFC,
            0x242EAD38,
            0xF16D5486,
            0x5D469491,
            0xB92A3E23,
            0x00802460,
        ];

        private static ReadOnlySpan<int> Pow5TableIndices32 =>
        [
            0, 2, 5, 9, 15, 26, 46, 85,
        ];

        private static ReadOnlySpan<uint> Pow5TableStorage32 =>
        [
            1, 0x0005F5E1, // 5^8

            2, // 5^16
            0x86F26FC1,
            0x00000023,

            3, // 5^32
            0x85ACEF81,
            0x2D6D415B,
            0x000004EE,

            5, // 5^64
            0xBF6A1F01,
            0x6E38ED64,
            0xDAA797ED,
            0xE93FF9F4,
            0x00184F03,

            10, // 5^128
            0x2E953E01,
            0x03DF9909,
            0x0F1538FD,
            0x2374E42F,
            0xD3CFF5EC,
            0xC404DC08,
            0xBCCDB0DA,
            0xA6337F19,
            0xE91F2603,
            0x0000024E,

            19, // 5^256
            0x982E7C01,
            0xBED3875B,
            0xD8D99F72,
            0x12152F87,
            0x6BDE50C6,
            0xCF4A6E70,
            0xD595D80F,
            0x26B2716E,
            0xADC666B0,
            0x1D153624,
            0x3C42D35A,
            0x63FF540E,
            0xCC5573C0,
            0x65F9EF17,
            0x55BC28F2,
            0x80DCC7F7,
            0xF46EEDDC,
            0x5FDCEFCE,
            0x000553F7,

            38, // 5^512
            0xFC6CF801,
            0x77F27267,
            0x8F9546DC,
            0x5D96976F,
            0xB83A8A97,
            0xC31E1AD9,
            0x46C40513,
            0x94E65747,
            0xC88976C1,
            0x4475B579,
            0x28F8733B,
            0xAA1DA1BF,
            0x703ED321,
            0x1E25CFEA,
            0xB21A2F22,
            0xBC51FB2E,
            0x96E14F5D,
            0xBFA3EDAC,
            0x329C57AE,
            0xE7FC7153,
            0xC3FC0695,
            0x85A91924,
            0xF95F635E,
            0xB2908EE0,
            0x93ABADE4,
            0x1366732A,
            0x9449775C,
            0x69BE5B0E,
            0x7343AFAC,
            0xB099BC81,
            0x45A71D46,
            0xA2699748,
            0x8CB07303,
            0x8A0B1F13,
            0x8CAB8A97,
            0xC1D238D9,
            0x633415D4,
            0x0000001C,

            75, // 5^1024
            0x2919F001,
            0xF55B2B72,
            0x6E7C215B,
            0x1EC29F86,
            0x991C4E87,
            0x15C51A88,
            0x140AC535,
            0x4C7D1E1A,
            0xCC2CD819,
            0x0ED1440E,
            0x896634EE,
            0x7DE16CFB,
            0x1E43F61F,
            0x9FCE837D,
            0x231D2B9C,
            0x233E55C7,
            0x65DC60D7,
            0xF451218B,
            0x1C5CD134,
            0xC9635986,
            0x922BBB9F,
            0xA7E89431,
            0x9F9F2A07,
            0x62BE695A,
            0x8E1042C4,
            0x045B7A74,
            0x1ABE1DE3,
            0x8AD822A5,
            0xBA34C411,
            0xD814B505,
            0xBF3FDEB3,
            0x8FC51A16,
            0xB1B896BC,
            0xF56DEEEC,
            0x31FB6BFD,
            0xB6F4654B,
            0x101A3616,
            0x6B7595FB,
            0xDC1A47FE,
            0x80D98089,
            0x80BDA5A5,
            0x9A202882,
            0x31EB0F66,
            0xFC8F1F90,
            0x976A3310,
            0xE26A7B7E,
            0xDF68368A,
            0x3CE3A0B8,
            0x8E4262CE,
            0x75A351A2,
            0x6CB0B6C9,
            0x44597583,
            0x31B5653F,
            0xC356E38A,
            0x35FAABA6,
            0x0190FBA0,
            0x9FC4ED52,
            0x88BC491B,
            0x1640114A,
            0x005B8041,
            0xF4F3235E,
            0x1E8D4649,
            0x36A8DE06,
            0x73C55349,
            0xA7E6BD2A,
            0xC1A6970C,
            0x47187094,
            0xD2DB49EF,
            0x926C3F5B,
            0xAE6209D4,
            0x2D433949,
            0x34F4A3C6,
            0xD4305D94,
            0xD9D61A05,
            0x00000325,
        ];

        private static ReadOnlySpan<int> Pow7TableIndices32 =>
        [
            0, 2, 5, 9, 16, 29, 53, 99,
        ];

        private static ReadOnlySpan<uint> Pow7TableStorage32 =>
        [
            1, 0x0057F6C1, // 7^8

            2, // 7^16
            0xA5057D81,
            0x00001E39,

            3, // 7^32
            0x6E913B01,
            0x303C3358,
            0x03918FA8,

            6, // 7^64
            0xC0BB7601,
            0x199417C8,
            0x80E1341E,
            0xD63B78E7,
            0xFE4561C8,
            0x000CBC21,

            12, // 7^128
            0x1BDAEC01,
            0x085E49D7,
            0x799B3618,
            0x5BF188FE,
            0x2649D046,
            0xC8A2B5C3,
            0x3192BAF6,
            0xF3CC3D60,
            0x4F5895F9,
            0x9CE406DA,
            0x2D71C87A,
            0x000000A2,

            23, // 7^256
            0x0145D801,
            0x0814E4AD,
            0x10D53258,
            0x0CFAAF9C,
            0xE29D44BD,
            0x6971852F,
            0xB6851623,
            0x402F567B,
            0x430C356C,
            0x800266CD,
            0x40BD987C,
            0xBDDC35A5,
            0xC1949DB2,
            0x7BD35A98,
            0x5086ABE3,
            0x94AE92E2,
            0x54F8EAC1,
            0x6578E5B1,
            0x25627743,
            0x6CEF58CB,
            0x44FF27E1,
            0x8C12EE3C,
            0x000066BD,

            45, // 7^512
            0x28CBB001,
            0xF1515818,
            0x11701448,
            0xE46393E1,
            0xC6A24D82,
            0x1C3797F8,
            0x12F6E4E8,
            0x05631AA8,
            0x3D9CF244,
            0xA2673CAB,
            0xD64A7DEA,
            0xD8519FF9,
            0xFAE3E411,
            0xEC5AA0BD,
            0x856FCE3F,
            0xBBFFCDEE,
            0xF189DC38,
            0x124D6232,
            0x13036414,
            0xBBFD8107,
            0xBC240CAF,
            0xDB636E5F,
            0xF69E4EC1,
            0xAB40AD83,
            0x06A732FA,
            0xA09E224A,
            0x008E3FB2,
            0x5D3CED80,
            0x0208291E,
            0xC1A76E7D,
            0x14E86615,
            0xE464A71E,
            0x76D1E82E,
            0x3889E7D5,
            0xA96476D4,
            0x2221C72B,
            0xFB552E43,
            0x9DD4ED7B,
            0xDF7E38DA,
            0xAC9FC6BF,
            0xAC5ABD17,
            0x975E7C3E,
            0x7E2D6BA4,
            0x367081F1,
            0x293B97F7,

            90, // 7^1024
            0xEA976001,
            0xF853F940,
            0x435D192A,
            0x2C80DD09,
            0x7D01C2BF,
            0x0DC1F204,
            0x0FA529F6,
            0xB74658A8,
            0xFCD87A0C,
            0xFA444EBF,
            0x45A0736D,
            0xE467C820,
            0x25DD9342,
            0xECE02498,
            0xA08CC39A,
            0xF187913C,
            0x0D5C07C2,
            0x0D5BA7F0,
            0x6D315D0A,
            0x34B6FF30,
            0xF811E85F,
            0x96D4F3E4,
            0x694DA8FA,
            0xDE90FD65,
            0x82BB48DE,
            0xD932761C,
            0x958B1FE5,
            0x49C9470F,
            0x31476EDA,
            0x11F56337,
            0x50C95C23,
            0x3C650D44,
            0x0C044032,
            0xEFB5A1DF,
            0x5E7EAE92,
            0xA1603948,
            0x71496504,
            0x719082CC,
            0x63E1CD52,
            0x4E1C0777,
            0xD0130B33,
            0x61A47EAB,
            0xF03B33B2,
            0xCDF7A64B,
            0x8DEBD3C8,
            0xC6F08D5A,
            0x10A3209C,
            0x3A85B190,
            0xFDAEABBB,
            0x8B6C2A33,
            0x24F484F3,
            0xBE53E6C0,
            0x3D125C65,
            0x4CDC9E26,
            0xDFCA20EF,
            0xB9E820A4,
            0xBFF8AFDA,
            0x5025AD27,
            0x443BD345,
            0x069BF53B,
            0x984BE5D1,
            0x61C13DF9,
            0x0FDCE741,
            0x0F96C2C2,
            0x9A6A3981,
            0xB51672B3,
            0xF9CF7556,
            0xF196580A,
            0xBD72C9ED,
            0xCC813D93,
            0x5A07729A,
            0x89C81F85,
            0x19113A71,
            0xE76ABACB,
            0x4C86F713,
            0xFF8667E2,
            0x11C256E3,
            0x58E83221,
            0xB11D0C06,
            0x43C26500,
            0x3DD2DC55,
            0x0808AAC1,
            0x2D7F5FAB,
            0xB7E1C05B,
            0xCB2AF962,
            0x1D0FCC36,
            0x3D7AC1A7,
            0x11B40089,
            0x9598B26E,
            0x06A4248C,
        ];

        internal static ReadOnlySpan<int> Pow3TableIndices
            => nint.Size == 8 ? Pow3TableIndices64 : Pow3TableIndices32;

        internal static ReadOnlySpan<nuint> Pow3Table
            => nint.Size == 8
                ? MemoryMarshal.Cast<ulong, nuint>(Pow3TableStorage64)
                : MemoryMarshal.Cast<uint, nuint>(Pow3TableStorage32);

        internal static int Pow3TableStartIndex => nint.Size == 8 ? 5 : 4;

        internal static ReadOnlySpan<int> Pow5TableIndices
            => nint.Size == 8 ? Pow5TableIndices64 : Pow5TableIndices32;

        internal static ReadOnlySpan<nuint> Pow5Table
            => nint.Size == 8
                ? MemoryMarshal.Cast<ulong, nuint>(Pow5TableStorage64)
                : MemoryMarshal.Cast<uint, nuint>(Pow5TableStorage32);

        internal static int Pow5TableStartIndex => nint.Size == 8 ? 4 : 3;

        internal static ReadOnlySpan<int> Pow7TableIndices
            => nint.Size == 8 ? Pow7TableIndices64 : Pow7TableIndices32;

        internal static ReadOnlySpan<nuint> Pow7Table
            => nint.Size == 8
                ? MemoryMarshal.Cast<ulong, nuint>(Pow7TableStorage64)
                : MemoryMarshal.Cast<uint, nuint>(Pow7TableStorage32);

        internal static int Pow7TableStartIndex => nint.Size == 8 ? 4 : 3;

        internal static ReadOnlySpan<nuint> GetPower(
            ReadOnlySpan<int> indices,
            ReadOnlySpan<nuint> powers,
            int index)
        {
            int tableIndex = indices[index];
            int length = checked((int)powers[tableIndex]);
            return powers.Slice(tableIndex + 1, length);
        }

    }
}
