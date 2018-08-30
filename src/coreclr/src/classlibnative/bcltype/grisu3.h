// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: grisu3.h
//

//

#ifndef _GRISU3_H
#define _GRISU3_H

#include "diyfp.h"

#ifdef _MSC_VER
#define _signbit signbit
#define _signbitf signbit
#endif

struct PowerOfTen
{
    UINT64 significand;
    INT16 binaryExponent;
    INT16 decimalExponent;
};

class Grisu3
{
public:
    static bool Run(double value, int count, int* dec, int* sign, wchar_t* digits);

private:
    Grisu3();
    Grisu3(const Grisu3&);
    Grisu3& operator=(const Grisu3&);

    static int KComp(int e);
    static void CachedPower(int k, DiyFp* cmk, int* decimalExponent);
    static bool DigitGen(const DiyFp& mp, int count, wchar_t* buffer, int* len, int* k);
    static bool RoundWeed(wchar_t* buffer, int len, UINT64 rest, UINT64 tenKappa, UINT64 ulp, int* kappa);
    static void BiggestPowerTenLessThanOrEqualTo(UINT32 number, int bits, UINT32 *power, int *exponent);

    // 1/lg(10)
    static const double D_1_LOG2_10;

    static const int ALPHA = -59;
    static const int GAMA = -32;

    static const UINT32 TEN4 = 10000;
    static const UINT32 TEN5 = 100000;
    static const UINT32 TEN6 = 1000000;
    static const UINT32 TEN7 = 10000000;
    static const UINT32 TEN8 = 100000000;
    static const UINT32 TEN9 = 1000000000; 

    static const int CACHED_POWER_OF_TEN_NUM = 10;
    static constexpr UINT32 m_cachedPowerOfTen[CACHED_POWER_OF_TEN_NUM] = {
        1,              // 10^0
        10,             // 10^1
        100,            // 10^2
        1000,           // 10^3
        10000,          // 10^4
        100000,         // 10^5
        1000000,        // 10^6
        10000000,       // 10^7
        100000000,      // 10^8
        1000000000     // 10^9
    };

    static const int POWER_DECIMAL_EXPONENT_DISTANCE = 8;
    static const int POWER_MIN_DECIMAL_EXPONENT = -348;
    static const int POWER_MAX_DECIMAL_EXPONENT = 340;
    static const int POWER_OFFSET = -POWER_MIN_DECIMAL_EXPONENT;
    static const int CACHED_POWER_NUM = 87;
    static constexpr PowerOfTen m_cachedPowers[CACHED_POWER_NUM] = {
        { 0xfa8fd5a0081c0288, -1220, POWER_MIN_DECIMAL_EXPONENT },
        { 0xbaaee17fa23ebf76, -1193, -340 },
        { 0x8b16fb203055ac76, -1166, -332 },
        { 0xcf42894a5dce35ea, -1140, -324 },
        { 0x9a6bb0aa55653b2d, -1113, -316 },
        { 0xe61acf033d1a45df, -1087, -308 },
        { 0xab70fe17c79ac6ca, -1060, -300 },
        { 0xff77b1fcbebcdc4f, -1034, -292 },
        { 0xbe5691ef416bd60c, -1007, -284 },
        { 0x8dd01fad907ffc3c, -980, -276 },
        { 0xd3515c2831559a83, -954, -268 },
        { 0x9d71ac8fada6c9b5, -927, -260 },
        { 0xea9c227723ee8bcb, -901, -252 },
        { 0xaecc49914078536d, -874, -244 },
        { 0x823c12795db6ce57, -847, -236 },
        { 0xc21094364dfb5637, -821, -228 },
        { 0x9096ea6f3848984f, -794, -220 },
        { 0xd77485cb25823ac7, -768, -212 },
        { 0xa086cfcd97bf97f4, -741, -204 },
        { 0xef340a98172aace5, -715, -196 },
        { 0xb23867fb2a35b28e, -688, -188 },
        { 0x84c8d4dfd2c63f3b, -661, -180 },
        { 0xc5dd44271ad3cdba, -635, -172 },
        { 0x936b9fcebb25c996, -608, -164 },
        { 0xdbac6c247d62a584, -582, -156 },
        { 0xa3ab66580d5fdaf6, -555, -148 },
        { 0xf3e2f893dec3f126, -529, -140 },
        { 0xb5b5ada8aaff80b8, -502, -132 },
        { 0x87625f056c7c4a8b, -475, -124 },
        { 0xc9bcff6034c13053, -449, -116 },
        { 0x964e858c91ba2655, -422, -108 },
        { 0xdff9772470297ebd, -396, -100 },
        { 0xa6dfbd9fb8e5b88f, -369, -92 },
        { 0xf8a95fcf88747d94, -343, -84 },
        { 0xb94470938fa89bcf, -316, -76 },
        { 0x8a08f0f8bf0f156b, -289, -68 },
        { 0xcdb02555653131b6, -263, -60 },
        { 0x993fe2c6d07b7fac, -236, -52 },
        { 0xe45c10c42a2b3b06, -210, -44 },
        { 0xaa242499697392d3, -183, -36 },
        { 0xfd87b5f28300ca0e, -157, -28 },
        { 0xbce5086492111aeb, -130, -20 },
        { 0x8cbccc096f5088cc, -103, -12 },
        { 0xd1b71758e219652c, -77, -4 },
        { 0x9c40000000000000, -50, 4 },
        { 0xe8d4a51000000000, -24, 12 },
        { 0xad78ebc5ac620000, 3, 20 },
        { 0x813f3978f8940984, 30, 28 },
        { 0xc097ce7bc90715b3, 56, 36 },
        { 0x8f7e32ce7bea5c70, 83, 44 },
        { 0xd5d238a4abe98068, 109, 52 },
        { 0x9f4f2726179a2245, 136, 60 },
        { 0xed63a231d4c4fb27, 162, 68 },
        { 0xb0de65388cc8ada8, 189, 76 },
        { 0x83c7088e1aab65db, 216, 84 },
        { 0xc45d1df942711d9a, 242, 92 },
        { 0x924d692ca61be758, 269, 100 },
        { 0xda01ee641a708dea, 295, 108 },
        { 0xa26da3999aef774a, 322, 116 },
        { 0xf209787bb47d6b85, 348, 124 },
        { 0xb454e4a179dd1877, 375, 132 },
        { 0x865b86925b9bc5c2, 402, 140 },
        { 0xc83553c5c8965d3d, 428, 148 },
        { 0x952ab45cfa97a0b3, 455, 156 },
        { 0xde469fbd99a05fe3, 481, 164 },
        { 0xa59bc234db398c25, 508, 172 },
        { 0xf6c69a72a3989f5c, 534, 180 },
        { 0xb7dcbf5354e9bece, 561, 188 },
        { 0x88fcf317f22241e2, 588, 196 },
        { 0xcc20ce9bd35c78a5, 614, 204 },
        { 0x98165af37b2153df, 641, 212 },
        { 0xe2a0b5dc971f303a, 667, 220 },
        { 0xa8d9d1535ce3b396, 694, 228 },
        { 0xfb9b7cd9a4a7443c, 720, 236 },
        { 0xbb764c4ca7a44410, 747, 244 },
        { 0x8bab8eefb6409c1a, 774, 252 },
        { 0xd01fef10a657842c, 800, 260 },
        { 0x9b10a4e5e9913129, 827, 268 },
        { 0xe7109bfba19c0c9d, 853, 276 },
        { 0xac2820d9623bf429, 880, 284 },
        { 0x80444b5e7aa7cf85, 907, 292 },
        { 0xbf21e44003acdd2d, 933, 300 },
        { 0x8e679c2f5e44ff8f, 960, 308 },
        { 0xd433179d9c8cb841, 986, 316 },
        { 0x9e19db92b4e31ba9, 1013, 324 },
        { 0xeb96bf6ebadf77d9, 1039, 332 },
        { 0xaf87023b9bf0ee6b, 1066, POWER_MAX_DECIMAL_EXPONENT }
    };
};

#endif
