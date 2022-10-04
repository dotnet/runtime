// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

#pragma warning disable SA1001

// This file contains the handling of Windows OS specific culture features.

namespace System.Globalization
{
    internal static partial class IcuLocaleData
    {
        /*
        // Program used to generate and validate the culture data
        // input data needs to come sorted

        private const int NUMERIC_LOCALE_DATA_COUNT_PER_ROW = 9;

        internal const int CommaSep = 0 << 4;
        internal const int SemicolonSep = 1 << 4;
        internal const int ArabicCommaSep = 2 << 4;
        internal const int ArabicSemicolonSep = 3 << 4;
        internal const int DoubleCommaSep = 4 << 4;

        private const int CulturesCount = 864;
        // s_nameIndexToNumericData is mapping from index in s_localeNamesIndices to locale data.
        // each row in the table will have the following data:
        //      Lcid, Ansi codepage, Oem codepage, MAC codepage, EBCDIC codepage, Geo Id, Digit Substitution | ListSeparator, specific locale index, Console locale index
        private static readonly int[] s_nameIndexToNumericData = new int[CulturesCount * NUMERIC_LOCALE_DATA_COUNT_PER_ROW]
        {
            // Lcid,  Ansi CP, Oem CP, MAC CP, EBCDIC CP, Geo Id, digit substitution | ListSeparator, Specific culture index, Console locale index  // index - locale name
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 3   , 240 , // 0    - aa
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3e  , 1 | SemicolonSep      , 1   , 240 , // 1    - aa-dj
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x47  , 1 | SemicolonSep      , 2   , 240 , // 2    - aa-er
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 3   , 240 , // 3    - aa-et
            0x36   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 6   , 6   , // 4    - af
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xfe  , 1 | SemicolonSep      , 5   , 240 , // 5    - af-na
            0x436  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 6   , 6   , // 6    - af-za
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 8   , 240 , // 7    - agq
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 8   , 240 , // 8    - agq-cm
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x59  , 1 | SemicolonSep      , 10  , 240 , // 9    - ak
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x59  , 1 | SemicolonSep      , 10  , 240 , // 10   - ak-gh
            0x5e   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 12  , 143 , // 11   - am
            0x45e  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 12  , 143 , // 12   - am-et
            0x1    , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xcd  , 0 | SemicolonSep      , 33  , 143 , // 13   - ar
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x989e, 0 | SemicolonSep      , 14  , 240 , // 14   - ar-001
            0x3801 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xe0  , 0 | SemicolonSep      , 15  , 143 , // 15   - ar-ae
            0x3c01 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x11  , 0 | SemicolonSep      , 16  , 143 , // 16   - ar-bh
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x3e  , 0 | SemicolonSep      , 17  , 240 , // 17   - ar-dj
            0x1401 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x4   , 1 | SemicolonSep      , 18  , 300 , // 18   - ar-dz
            0xc01  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x43  , 0 | SemicolonSep      , 19  , 143 , // 19   - ar-eg
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x47  , 0 | SemicolonSep      , 20  , 240 , // 20   - ar-er
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x75  , 0 | SemicolonSep      , 21  , 240 , // 21   - ar-il
            0x801  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x79  , 0 | SemicolonSep      , 22  , 143 , // 22   - ar-iq
            0x2c01 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x7e  , 0 | SemicolonSep      , 23  , 143 , // 23   - ar-jo
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x32  , 0 | SemicolonSep      , 24  , 240 , // 24   - ar-km
            0x3401 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x88  , 0 | SemicolonSep      , 25  , 143 , // 25   - ar-kw
            0x3001 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x8b  , 0 | SemicolonSep      , 26  , 143 , // 26   - ar-lb
            0x1001 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x94  , 1 | SemicolonSep      , 27  , 143 , // 27   - ar-ly
            0x1801 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x9f  , 1 | SemicolonSep      , 28  , 300 , // 28   - ar-ma
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xa2  , 0 | SemicolonSep      , 29  , 240 , // 29   - ar-mr
            0x2001 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xa4  , 0 | SemicolonSep      , 30  , 143 , // 30   - ar-om
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xb8  , 0 | SemicolonSep      , 31  , 240 , // 31   - ar-ps
            0x4001 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xc5  , 0 | SemicolonSep      , 32  , 143 , // 32   - ar-qa
            0x401  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xcd  , 0 | SemicolonSep      , 33  , 143 , // 33   - ar-sa
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xdb  , 0 | SemicolonSep      , 34  , 240 , // 34   - ar-sd
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xd8  , 0 | SemicolonSep      , 35  , 240 , // 35   - ar-so
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x114 , 0 | SemicolonSep      , 36  , 240 , // 36   - ar-ss
            0x2801 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xde  , 0 | SemicolonSep      , 37  , 143 , // 37   - ar-sy
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x29  , 0 | SemicolonSep      , 38  , 240 , // 38   - ar-td
            0x1c01 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xea  , 1 | SemicolonSep      , 39  , 300 , // 39   - ar-tn
            0x2401 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x105 , 0 | SemicolonSep      , 40  , 143 , // 40   - ar-ye
            0x7a   , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x2e  , 1 | CommaSep          , 42  , 42  , // 41   - arn
            0x47a  , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x2e  , 1 | CommaSep          , 42  , 42  , // 42   - arn-cl
            0x4d   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 44  , 143 , // 43   - as
            0x44d  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 44  , 143 , // 44   - as-in
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 46  , 240 , // 45   - asa
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 46  , 240 , // 46   - asa-tz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd9  , 1 | SemicolonSep      , 48  , 240 , // 47   - ast
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd9  , 1 | SemicolonSep      , 48  , 240 , // 48   - ast-es
            0x2c   , 0x4e6 , 0x359 , 0x2761, 0x51a9, 0x5   , 1 | SemicolonSep      , 53  , 53  , // 49   - az
            0x742c , 0x4e3 , 0x362 , 0x2717, 0x5190, 0x5   , 1 | SemicolonSep      , 51  , 51  , // 50   - az-cyrl
            0x82c  , 0x4e3 , 0x362 , 0x2717, 0x5190, 0x5   , 1 | SemicolonSep      , 51  , 51  , // 51   - az-cyrl-az
            0x782c , 0x4e6 , 0x359 , 0x2761, 0x51a9, 0x5   , 1 | SemicolonSep      , 53  , 53  , // 52   - az-latn
            0x42c  , 0x4e6 , 0x359 , 0x2761, 0x51a9, 0x5   , 1 | SemicolonSep      , 53  , 53  , // 53   - az-latn-az
            0x6d   , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xcb  , 1 | SemicolonSep      , 55  , 55  , // 54   - ba
            0x46d  , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xcb  , 1 | SemicolonSep      , 55  , 55  , // 55   - ba-ru
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 57  , 240 , // 56   - bas
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 57  , 240 , // 57   - bas-cm
            0x23   , 0x4e3 , 0x362 , 0x2717, 0x1f4 , 0x1d  , 1 | SemicolonSep      , 59  , 59  , // 58   - be
            0x423  , 0x4e3 , 0x362 , 0x2717, 0x1f4 , 0x1d  , 1 | SemicolonSep      , 59  , 59  , // 59   - be-by
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x107 , 1 | SemicolonSep      , 61  , 240 , // 60   - bem
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x107 , 1 | SemicolonSep      , 61  , 240 , // 61   - bem-zm
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 63  , 240 , // 62   - bez
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 63  , 240 , // 63   - bez-tz
            0x2    , 0x4e3 , 0x362 , 0x2717, 0x5221, 0x23  , 1 | SemicolonSep      , 65  , 65  , // 64   - bg
            0x402  , 0x4e3 , 0x362 , 0x2717, 0x5221, 0x23  , 1 | SemicolonSep      , 65  , 65  , // 65   - bg-bg
            0x66   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xaf  , 1 | SemicolonSep      , 67  , 240 , // 66   - bin
            0x466  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xaf  , 1 | SemicolonSep      , 67  , 240 , // 67   - bin-ng
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9d  , 1 | SemicolonSep      , 70  , 240 , // 68   - bm
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9d  , 1 | SemicolonSep      , 70  , 240 , // 69   - bm-latn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9d  , 1 | SemicolonSep      , 70  , 240 , // 70   - bm-latn-ml
            0x45   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x17  , 1 | CommaSep          , 72  , 143 , // 71   - bn
            0x845  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x17  , 1 | CommaSep          , 72  , 143 , // 72   - bn-bd
            0x445  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 73  , 143 , // 73   - bn-in
            0x51   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2d  , 1 | CommaSep          , 75  , 143 , // 74   - bo
            0x451  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2d  , 1 | CommaSep          , 75  , 143 , // 75   - bo-cn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | SemicolonSep      , 76  , 240 , // 76   - bo-in
            0x7e   , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x54  , 1 | SemicolonSep      , 78  , 78  , // 77   - br
            0x47e  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x54  , 1 | SemicolonSep      , 78  , 78  , // 78   - br-fr
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | SemicolonSep      , 80  , 240 , // 79   - brx
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | SemicolonSep      , 80  , 240 , // 80   - brx-in
            0x781a , 0x4e2 , 0x354 , 0x2762, 0x366 , 0x19  , 1 | SemicolonSep      , 85  , 85  , // 81   - bs
            0x641a , 0x4e3 , 0x357 , 0x2762, 0x366 , 0x19  , 1 | SemicolonSep      , 83  , 83  , // 82   - bs-cyrl
            0x201a , 0x4e3 , 0x357 , 0x2762, 0x366 , 0x19  , 1 | SemicolonSep      , 83  , 83  , // 83   - bs-cyrl-ba
            0x681a , 0x4e2 , 0x354 , 0x2762, 0x366 , 0x19  , 1 | SemicolonSep      , 85  , 85  , // 84   - bs-latn
            0x141a , 0x4e2 , 0x354 , 0x2762, 0x366 , 0x19  , 1 | SemicolonSep      , 85  , 85  , // 85   - bs-latn-ba
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x47  , 1 | SemicolonSep      , 87  , 240 , // 86   - byn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x47  , 1 | SemicolonSep      , 87  , 240 , // 87   - byn-er
            0x3    , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd9  , 1 | SemicolonSep      , 90  , 90  , // 88   - ca
            0x1000 , 0x4e4 , 0x352 , 0x2   , 0x1f4 , 0x8   , 1 | SemicolonSep      , 89  , 240 , // 89   - ca-ad
            0x403  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd9  , 1 | SemicolonSep      , 90  , 90  , // 90   - ca-es
            0x803  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd9  , 1 | SemicolonSep      , 91  , 90  , // 91   - ca-es-valencia
            0x1000 , 0x4e4 , 0x352 , 0x2   , 0x1f4 , 0x54  , 1 | SemicolonSep      , 92  , 240 , // 92   - ca-fr
            0x1000 , 0x4e4 , 0x352 , 0x2   , 0x1f4 , 0x76  , 1 | SemicolonSep      , 93  , 240 , // 93   - ca-it
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xcb  , 1 | SemicolonSep      , 95  , 240 , // 94   - ce
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xcb  , 1 | SemicolonSep      , 95  , 240 , // 95   - ce-ru
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 97  , 240 , // 96   - cgg
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 97  , 240 , // 97   - cgg-ug
            0x5c   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf4  , 1 | CommaSep          , 100 , 240 , // 98   - chr
            0x7c5c , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf4  , 1 | CommaSep          , 100 , 240 , // 99   - chr-cher
            0x45c  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf4  , 1 | CommaSep          , 100 , 240 , // 100  - chr-cher-us
            0x83   , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x54  , 1 | SemicolonSep      , 102 , 102 , // 101  - co
            0x483  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x54  , 1 | SemicolonSep      , 102 , 102 , // 102  - co-fr
            0x5    , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x4b  , 1 | SemicolonSep      , 104 , 104 , // 103  - cs
            0x405  , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x4b  , 1 | SemicolonSep      , 104 , 104 , // 104  - cs-cz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xcb  , 1 | SemicolonSep      , 106 , 240 , // 105  - cu
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xcb  , 1 | SemicolonSep      , 106 , 240 , // 106  - cu-ru
            0x52   , 0x4e4 , 0x352 , 0x2710, 0x4f3d, 0xf2  , 1 | SemicolonSep      , 108 , 108 , // 107  - cy
            0x452  , 0x4e4 , 0x352 , 0x2710, 0x4f3d, 0xf2  , 1 | SemicolonSep      , 108 , 108 , // 108  - cy-gb
            0x6    , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0x3d  , 1 | SemicolonSep      , 110 , 110 , // 109  - da
            0x406  , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0x3d  , 1 | SemicolonSep      , 110 , 110 , // 110  - da-dk
            0x1000 , 0x4e4 , 0x352 , 0x2   , 0x1f4 , 0x5d  , 1 | SemicolonSep      , 111 , 240 , // 111  - da-gl
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 113 , 240 , // 112  - dav
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 113 , 240 , // 113  - dav-ke
            0x7    , 0x4e4 , 0x352 , 0x2710, 0x4f31, 0x5e  , 1 | SemicolonSep      , 118 , 118 , // 114  - de
            0xc07  , 0x4e4 , 0x352 , 0x2710, 0x4f31, 0xe   , 1 | SemicolonSep      , 115 , 115 , // 115  - de-at
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f31, 0x15  , 1 | SemicolonSep      , 116 , 240 , // 116  - de-be
            0x807  , 0x4e4 , 0x352 , 0x2710, 0x4f31, 0xdf  , 1 | SemicolonSep      , 117 , 117 , // 117  - de-ch
            0x407  , 0x4e4 , 0x352 , 0x2710, 0x4f31, 0x5e  , 1 | SemicolonSep      , 118 , 118 , // 118  - de-de
            0x10407, 0x4e4 , 0x352 , 0x2710, 0x4f31, 0x5e  , 1 | SemicolonSep      , 118 , 118 , // 119  - de-de_phoneb
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x76  , 1 | SemicolonSep      , 120 , 240 , // 120  - de-it
            0x1407 , 0x4e4 , 0x352 , 0x2710, 0x4f31, 0x91  , 1 | SemicolonSep      , 121 , 121 , // 121  - de-li
            0x1007 , 0x4e4 , 0x352 , 0x2710, 0x4f31, 0x93  , 1 | SemicolonSep      , 122 , 122 , // 122  - de-lu
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xad  , 1 | SemicolonSep      , 124 , 240 , // 123  - dje
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xad  , 1 | SemicolonSep      , 124 , 240 , // 124  - dje-ne
            0x7c2e , 0x4e4 , 0x352 , 0x2710, 0x366 , 0x5e  , 1 | SemicolonSep      , 126 , 126 , // 125  - dsb
            0x82e  , 0x4e4 , 0x352 , 0x2710, 0x366 , 0x5e  , 1 | SemicolonSep      , 126 , 126 , // 126  - dsb-de
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 128 , 240 , // 127  - dua
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 128 , 240 , // 128  - dua-cm
            0x65   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa5  , 1 | ArabicCommaSep    , 130 , 143 , // 129  - dv
            0x465  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa5  , 1 | ArabicCommaSep    , 130 , 143 , // 130  - dv-mv
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd2  , 1 | SemicolonSep      , 132 , 240 , // 131  - dyo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd2  , 1 | SemicolonSep      , 132 , 240 , // 132  - dyo-sn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x22  , 2 | SemicolonSep      , 134 , 240 , // 133  - dz
            0xc51  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x22  , 2 | SemicolonSep      , 134 , 240 , // 134  - dz-bt
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 136 , 240 , // 135  - ebu
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 136 , 240 , // 136  - ebu-ke
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x59  , 1 | SemicolonSep      , 138 , 240 , // 137  - ee
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x59  , 1 | SemicolonSep      , 138 , 240 , // 138  - ee-gh
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xe8  , 1 | SemicolonSep      , 139 , 240 , // 139  - ee-tg
            0x8    , 0x4e5 , 0x2e1 , 0x2716, 0x4f31, 0x62  , 1 | SemicolonSep      , 142 , 142 , // 140  - el
            0x1000 , 0x4e5 , 0x2e1 , 0x2716, 0x4f31, 0x3b  , 1 | SemicolonSep      , 141 , 240 , // 141  - el-cy
            0x408  , 0x4e5 , 0x2e1 , 0x2716, 0x4f31, 0x62  , 1 | SemicolonSep      , 142 , 142 , // 142  - el-gr
            0x9    , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xf4  , 1 | CommaSep          , 240 , 240 , // 143  - en
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x989e, 1 | CommaSep          , 144 , 240 , // 144  - en-001
            0x2409 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x993248, 1 | CommaSep        , 145 , 145 , // 145  - en-029
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x292d, 1 | CommaSep          , 146 , 240 , // 146  - en-150
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x2   , 1 | CommaSep          , 147 , 240 , // 147  - en-ag
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x12c , 1 | CommaSep          , 148 , 240 , // 148  - en-ai
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xa   , 1 | CommaSep          , 149 , 240 , // 149  - en-as
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xe   , 1 | CommaSep          , 150 , 240 , // 150  - en-at
            0xc09  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xc   , 1 | CommaSep          , 151 , 151 , // 151  - en-au
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x12  , 1 | CommaSep          , 152 , 240 , // 152  - en-bb
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x15  , 1 | CommaSep          , 153 , 240 , // 153  - en-be
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x26  , 1 | CommaSep          , 154 , 240 , // 154  - en-bi
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x14  , 1 | CommaSep          , 155 , 240 , // 155  - en-bm
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x16  , 1 | CommaSep          , 156 , 240 , // 156  - en-bs
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x13  , 1 | CommaSep          , 157 , 240 , // 157  - en-bw
            0x2809 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x18  , 1 | CommaSep          , 158 , 158 , // 158  - en-bz
            0x1009 , 0x4e4 , 0x352 , 0x2710, 0x25  , 0x27  , 1 | CommaSep          , 159 , 159 , // 159  - en-ca
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x137 , 1 | CommaSep          , 160 , 240 , // 160  - en-cc
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xdf  , 1 | CommaSep          , 161 , 240 , // 161  - en-ch
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x138 , 1 | CommaSep          , 162 , 240 , // 162  - en-ck
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x31  , 1 | CommaSep          , 163 , 240 , // 163  - en-cm
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x135 , 1 | CommaSep          , 164 , 240 , // 164  - en-cx
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3b  , 1 | CommaSep          , 165 , 240 , // 165  - en-cy
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x5e  , 1 | CommaSep          , 166 , 240 , // 166  - en-de
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3d  , 1 | CommaSep          , 167 , 240 , // 167  - en-dk
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x3f  , 1 | CommaSep          , 168 , 240 , // 168  - en-dm
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x47  , 1 | CommaSep          , 169 , 240 , // 169  - en-er
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x4d  , 1 | CommaSep          , 170 , 240 , // 170  - en-fi
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x4e  , 1 | CommaSep          , 171 , 240 , // 171  - en-fj
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x13b , 1 | CommaSep          , 172 , 240 , // 172  - en-fk
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x50  , 1 | CommaSep          , 173 , 240 , // 173  - en-fm
            0x809  , 0x4e4 , 0x352 , 0x2710, 0x4f3d, 0xf2  , 1 | CommaSep          , 174 , 174 , // 174  - en-gb
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x5b  , 1 | CommaSep          , 175 , 240 , // 175  - en-gd
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x144 , 1 | CommaSep          , 176 , 240 , // 176  - en-gg
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x59  , 1 | CommaSep          , 177 , 240 , // 177  - en-gh
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x5a  , 1 | CommaSep          , 178 , 240 , // 178  - en-gi
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x56  , 1 | CommaSep          , 179 , 240 , // 179  - en-gm
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x142 , 1 | CommaSep          , 180 , 240 , // 180  - en-gu
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x65  , 1 | CommaSep          , 181 , 240 , // 181  - en-gy
            0x3c09 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x68  , 1 | CommaSep          , 182 , 240 , // 182  - en-hk
            0x3809 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x6f  , 1 | SemicolonSep      , 183 , 240 , // 183  - en-id
            0x1809 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x44  , 1 | CommaSep          , 184 , 184 , // 184  - en-ie
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x75  , 1 | CommaSep          , 185 , 240 , // 185  - en-il
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x3b16, 1 | CommaSep          , 186 , 240 , // 186  - en-im
            0x4009 , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0x71  , 1 | CommaSep          , 187 , 187 , // 187  - en-in
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x72  , 1 | CommaSep          , 188 , 240 , // 188  - en-io
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x148 , 1 | CommaSep          , 189 , 240 , // 189  - en-je
            0x2009 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x7c  , 1 | CommaSep          , 190 , 190 , // 190  - en-jm
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x81  , 1 | CommaSep          , 191 , 240 , // 191  - en-ke
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x85  , 1 | CommaSep          , 192 , 240 , // 192  - en-ki
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xcf  , 1 | CommaSep          , 193 , 240 , // 193  - en-kn
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x133 , 1 | CommaSep          , 194 , 240 , // 194  - en-ky
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xda  , 1 | CommaSep          , 195 , 240 , // 195  - en-lc
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x8e  , 1 | CommaSep          , 196 , 240 , // 196  - en-lr
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x92  , 1 | CommaSep          , 197 , 240 , // 197  - en-ls
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x95  , 1 | CommaSep          , 198 , 240 , // 198  - en-mg
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xc7  , 1 | CommaSep          , 199 , 240 , // 199  - en-mh
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x97  , 1 | CommaSep          , 200 , 240 , // 200  - en-mo
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x151 , 1 | CommaSep          , 201 , 240 , // 201  - en-mp
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x14c , 1 | CommaSep          , 202 , 240 , // 202  - en-ms
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xa3  , 1 | CommaSep          , 203 , 240 , // 203  - en-mt
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xa0  , 1 | CommaSep          , 204 , 240 , // 204  - en-mu
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x9c  , 1 | CommaSep          , 205 , 240 , // 205  - en-mw
            0x4409 , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xa7  , 1 | CommaSep          , 206 , 206 , // 206  - en-my
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xfe  , 1 | CommaSep          , 207 , 240 , // 207  - en-na
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x150 , 1 | CommaSep          , 208 , 240 , // 208  - en-nf
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xaf  , 1 | CommaSep          , 209 , 240 , // 209  - en-ng
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xb0  , 1 | CommaSep          , 210 , 240 , // 210  - en-nl
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xb4  , 1 | CommaSep          , 211 , 240 , // 211  - en-nr
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x14f , 1 | CommaSep          , 212 , 240 , // 212  - en-nu
            0x1409 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xb7  , 1 | CommaSep          , 213 , 213 , // 213  - en-nz
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xc2  , 1 | CommaSep          , 214 , 240 , // 214  - en-pg
            0x3409 , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0xc9  , 1 | CommaSep          , 215 , 215 , // 215  - en-ph
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xbe  , 1 | CommaSep          , 216 , 240 , // 216  - en-pk
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x153 , 1 | CommaSep          , 217 , 240 , // 217  - en-pn
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xca  , 1 | CommaSep          , 218 , 240 , // 218  - en-pr
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xc3  , 1 | CommaSep          , 219 , 240 , // 219  - en-pw
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xcc  , 1 | CommaSep          , 220 , 240 , // 220  - en-rw
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x1e  , 1 | CommaSep          , 221 , 240 , // 221  - en-sb
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd0  , 1 | CommaSep          , 222 , 240 , // 222  - en-sc
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xdb  , 1 | CommaSep          , 223 , 240 , // 223  - en-sd
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xdd  , 1 | CommaSep          , 224 , 240 , // 224  - en-se
            0x4809 , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xd7  , 1 | CommaSep          , 225 , 225 , // 225  - en-sg
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x157 , 1 | CommaSep          , 226 , 240 , // 226  - en-sh
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd4  , 1 | CommaSep          , 227 , 240 , // 227  - en-si
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd5  , 1 | CommaSep          , 228 , 240 , // 228  - en-sl
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x114 , 1 | CommaSep          , 229 , 240 , // 229  - en-ss
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x78f7, 1 | CommaSep          , 230 , 240 , // 230  - en-sx
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x104 , 1 | CommaSep          , 231 , 240 , // 231  - en-sz
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x15d , 1 | CommaSep          , 232 , 240 , // 232  - en-tc
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x15b , 1 | CommaSep          , 233 , 240 , // 233  - en-tk
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xe7  , 1 | CommaSep          , 234 , 240 , // 234  - en-to
            0x2c09 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xe1  , 1 | CommaSep          , 235 , 235 , // 235  - en-tt
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xec  , 1 | CommaSep          , 236 , 240 , // 236  - en-tv
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xef  , 1 | CommaSep          , 237 , 240 , // 237  - en-tz
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xf0  , 1 | CommaSep          , 238 , 240 , // 238  - en-ug
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x9a55d40,1 | CommaSep        , 239 , 240 , // 239  - en-um
            0x409  , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xf4  , 1 | CommaSep          , 240 , 240 , // 240  - en-us
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xf8  , 1 | CommaSep          , 241 , 240 , // 241  - en-vc
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x15f , 1 | CommaSep          , 242 , 240 , // 242  - en-vg
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xfc  , 1 | CommaSep          , 243 , 240 , // 243  - en-vi
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xae  , 1 | CommaSep          , 244 , 240 , // 244  - en-vu
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x103 , 1 | CommaSep          , 245 , 240 , // 245  - en-ws
            0x1c09 , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0xd1  , 1 | CommaSep          , 246 , 246 , // 246  - en-za
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x107 , 1 | CommaSep          , 247 , 240 , // 247  - en-zm
            0x3009 , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0x108 , 1 | CommaSep          , 248 , 248 , // 248  - en-zw
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x989e, 1 | SemicolonSep      , 250 , 240 , // 249  - eo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x989e, 1 | SemicolonSep      , 250 , 240 , // 250  - eo-001
            0xa    , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xd9  , 1 | SemicolonSep      , 262 , 262 , // 251  - es
            0x580a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x9a55d41, 1 | SemicolonSep   , 252 , 240 , // 252  - es-419
            0x2c0a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xb   , 1 | SemicolonSep      , 253 , 253 , // 253  - es-ar
            0x400a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x1a  , 1 | SemicolonSep      , 254 , 254 , // 254  - es-bo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x20  , 1 | SemicolonSep      , 255 , 240 , // 255  - es-br
            0x340a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x2e  , 1 | SemicolonSep      , 256 , 256 , // 256  - es-cl
            0x240a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x33  , 1 | SemicolonSep      , 257 , 257 , // 257  - es-co
            0x140a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x36  , 1 | SemicolonSep      , 258 , 258 , // 258  - es-cr
            0x5c0a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x38  , 1 | SemicolonSep      , 259 , 240 , // 259  - es-cu
            0x1c0a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x41  , 1 | SemicolonSep      , 260 , 260 , // 260  - es-do
            0x300a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x42  , 1 | SemicolonSep      , 261 , 261 , // 261  - es-ec
            0xc0a  , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xd9  , 1 | SemicolonSep      , 262 , 262 , // 262  - es-es
            0x40a  , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xd9  , 1 | SemicolonSep      , 263 , 263 , // 263  - es-es_tradnl
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x45  , 1 | SemicolonSep      , 264 , 240 , // 264  - es-gq
            0x100a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x63  , 1 | SemicolonSep      , 265 , 265 , // 265  - es-gt
            0x480a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x6a  , 1 | SemicolonSep      , 266 , 266 , // 266  - es-hn
            0x80a  , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xa6  , 1 | CommaSep          , 267 , 267 , // 267  - es-mx
            0x4c0a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xb6  , 1 | SemicolonSep      , 268 , 268 , // 268  - es-ni
            0x180a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xc0  , 1 | SemicolonSep      , 269 , 269 , // 269  - es-pa
            0x280a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xbb  , 1 | SemicolonSep      , 270 , 270 , // 270  - es-pe
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xc9  , 1 | SemicolonSep      , 271 , 240 , // 271  - es-ph
            0x500a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xca  , 1 | SemicolonSep      , 272 , 272 , // 272  - es-pr
            0x3c0a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xb9  , 1 | SemicolonSep      , 273 , 273 , // 273  - es-py
            0x440a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x48  , 1 | SemicolonSep      , 274 , 274 , // 274  - es-sv
            0x540a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xf4  , 1 | CommaSep          , 275 , 275 , // 275  - es-us
            0x380a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xf6  , 1 | SemicolonSep      , 276 , 276 , // 276  - es-uy
            0x200a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xf9  , 1 | SemicolonSep      , 277 , 277 , // 277  - es-ve
            0x25   , 0x4e9 , 0x307 , 0x272d, 0x1f4 , 0x46  , 1 | SemicolonSep      , 279 , 279 , // 278  - et
            0x425  , 0x4e9 , 0x307 , 0x272d, 0x1f4 , 0x46  , 1 | SemicolonSep      , 279 , 279 , // 279  - et-ee
            0x2d   , 0x4e4 , 0x352 , 0x2   , 0x1f4 , 0xd9  , 1 | SemicolonSep      , 281 , 240 , // 280  - eu
            0x42d  , 0x4e4 , 0x352 , 0x2   , 0x1f4 , 0xd9  , 1 | SemicolonSep      , 281 , 240 , // 281  - eu-es
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 283 , 240 , // 282  - ewo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 283 , 240 , // 283  - ewo-cm
            0x29   , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x74  , 0 | ArabicSemicolonSep, 285 , 143 , // 284  - fa
            0x429  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x74  , 0 | ArabicSemicolonSep, 285 , 143 , // 285  - fa-ir
            0x67   , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xd2  , 1 | SemicolonSep      , 290 , 290 , // 286  - ff
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x31  , 1 | SemicolonSep      , 287 , 240 , // 287  - ff-cm
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x64  , 1 | SemicolonSep      , 288 , 240 , // 288  - ff-gn
            0x7c67 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xd2  , 1 | SemicolonSep      , 290 , 290 , // 289  - ff-latn
            0x867  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xd2  , 1 | SemicolonSep      , 290 , 290 , // 290  - ff-latn-sn
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xa2  , 1 | SemicolonSep      , 291 , 240 , // 291  - ff-mr
            0x467  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xaf  , 1 | SemicolonSep      , 292 , 240 , // 292  - ff-ng
            0xb    , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0x4d  , 1 | SemicolonSep      , 294 , 294 , // 293  - fi
            0x40b  , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0x4d  , 1 | SemicolonSep      , 294 , 294 , // 294  - fi-fi
            0x64   , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0xc9  , 1 | SemicolonSep      , 296 , 296 , // 295  - fil
            0x464  , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0xc9  , 1 | SemicolonSep      , 296 , 296 , // 296  - fil-ph
            0x38   , 0x4e4 , 0x352 , 0x275f, 0x4f35, 0x51  , 1 | SemicolonSep      , 299 , 299 , // 297  - fo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3d  , 1 | SemicolonSep      , 298 , 240 , // 298  - fo-dk
            0x438  , 0x4e4 , 0x352 , 0x275f, 0x4f35, 0x51  , 1 | SemicolonSep      , 299 , 299 , // 299  - fo-fo
            0xc    , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x54  , 1 | SemicolonSep      , 316 , 316 , // 300  - fr
            0x1c0c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x993248, 1 | SemicolonSep    , 301 , 316 , // 301  - fr-029
            0x80c  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x15  , 1 | SemicolonSep      , 302 , 302 , // 302  - fr-be
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xf5  , 1 | SemicolonSep      , 303 , 240 , // 303  - fr-bf
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x26  , 1 | SemicolonSep      , 304 , 240 , // 304  - fr-bi
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x1c  , 1 | SemicolonSep      , 305 , 240 , // 305  - fr-bj
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x9a55c4f, 1 | SemicolonSep   , 306 , 240 , // 306  - fr-bl
            0xc0c  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x27  , 1 | SemicolonSep      , 307 , 307 , // 307  - fr-ca
            0x240c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x2c  , 1 | SemicolonSep      , 308 , 240 , // 308  - fr-cd
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x37  , 1 | SemicolonSep      , 309 , 240 , // 309  - fr-cf
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x2b  , 1 | SemicolonSep      , 310 , 240 , // 310  - fr-cg
            0x100c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xdf  , 1 | SemicolonSep      , 311 , 311 , // 311  - fr-ch
            0x300c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x77  , 1 | SemicolonSep      , 312 , 240 , // 312  - fr-ci
            0x2c0c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x31  , 1 | SemicolonSep      , 313 , 240 , // 313  - fr-cm
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x3e  , 1 | SemicolonSep      , 314 , 240 , // 314  - fr-dj
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x4   , 1 | SemicolonSep      , 315 , 240 , // 315  - fr-dz
            0x40c  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x54  , 1 | SemicolonSep      , 316 , 316 , // 316  - fr-fr
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x57  , 1 | SemicolonSep      , 317 , 240 , // 317  - fr-ga
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x13d , 1 | SemicolonSep      , 318 , 240 , // 318  - fr-gf
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x64  , 1 | SemicolonSep      , 319 , 240 , // 319  - fr-gn
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x141 , 1 | SemicolonSep      , 320 , 240 , // 320  - fr-gp
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x45  , 1 | SemicolonSep      , 321 , 240 , // 321  - fr-gq
            0x3c0c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x67  , 1 | SemicolonSep      , 322 , 240 , // 322  - fr-ht
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x32  , 1 | SemicolonSep      , 323 , 240 , // 323  - fr-km
            0x140c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x93  , 1 | SemicolonSep      , 324 , 324 , // 324  - fr-lu
            0x380c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x9f  , 1 | SemicolonSep      , 325 , 240 , // 325  - fr-ma
            0x180c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x9e  , 1 | SemicolonSep      , 326 , 326 , // 326  - fr-mc
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x7bda, 1 | SemicolonSep      , 327 , 240 , // 327  - fr-mf
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x95  , 1 | SemicolonSep      , 328 , 240 , // 328  - fr-mg
            0x340c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x9d  , 1 | SemicolonSep      , 329 , 240 , // 329  - fr-ml
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x14a , 1 | SemicolonSep      , 330 , 240 , // 330  - fr-mq
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xa2  , 1 | SemicolonSep      , 331 , 240 , // 331  - fr-mr
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xa0  , 1 | SemicolonSep      , 332 , 240 , // 332  - fr-mu
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x14e , 1 | SemicolonSep      , 333 , 240 , // 333  - fr-nc
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xad  , 1 | SemicolonSep      , 334 , 240 , // 334  - fr-ne
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x13e , 1 | SemicolonSep      , 335 , 240 , // 335  - fr-pf
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xce  , 1 | SemicolonSep      , 336 , 240 , // 336  - fr-pm
            0x200c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xc6  , 1 | SemicolonSep      , 337 , 240 , // 337  - fr-re
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xcc  , 1 | SemicolonSep      , 338 , 240 , // 338  - fr-rw
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xd0  , 1 | SemicolonSep      , 339 , 240 , // 339  - fr-sc
            0x280c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xd2  , 1 | SemicolonSep      , 340 , 240 , // 340  - fr-sn
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xde  , 1 | SemicolonSep      , 341 , 240 , // 341  - fr-sy
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x29  , 1 | SemicolonSep      , 342 , 240 , // 342  - fr-td
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xe8  , 1 | SemicolonSep      , 343 , 240 , // 343  - fr-tg
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xea  , 1 | SemicolonSep      , 344 , 240 , // 344  - fr-tn
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xae  , 1 | SemicolonSep      , 345 , 240 , // 345  - fr-vu
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x160 , 1 | SemicolonSep      , 346 , 240 , // 346  - fr-wf
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x14b , 1 | SemicolonSep      , 347 , 240 , // 347  - fr-yt
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x76  , 1 | SemicolonSep      , 349 , 240 , // 348  - fur
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x76  , 1 | SemicolonSep      , 349 , 240 , // 349  - fur-it
            0x62   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xb0  , 1 | SemicolonSep      , 351 , 351 , // 350  - fy
            0x462  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xb0  , 1 | SemicolonSep      , 351 , 351 , // 351  - fy-nl
            0x3c   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x44  , 1 | SemicolonSep      , 353 , 353 , // 352  - ga
            0x83c  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x44  , 1 | SemicolonSep      , 353 , 353 , // 353  - ga-ie
            0x91   , 0x4e4 , 0x352 , 0x2710, 0x4f3d, 0xf2  , 1 | SemicolonSep      , 355 , 355 , // 354  - gd
            0x491  , 0x4e4 , 0x352 , 0x2710, 0x4f3d, 0xf2  , 1 | SemicolonSep      , 355 , 355 , // 355  - gd-gb
            0x56   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd9  , 1 | SemicolonSep      , 357 , 357 , // 356  - gl
            0x456  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd9  , 1 | SemicolonSep      , 357 , 357 , // 357  - gl-es
            0x74   , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xb9  , 1 | CommaSep          , 359 , 359 , // 358  - gn
            0x474  , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xb9  , 1 | CommaSep          , 359 , 359 , // 359  - gn-py
            0x84   , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xdf  , 1 | SemicolonSep      , 361 , 240 , // 360  - gsw
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xdf  , 1 | SemicolonSep      , 361 , 240 , // 361  - gsw-ch
            0x484  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x54  , 1 | SemicolonSep      , 362 , 362 , // 362  - gsw-fr
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x91  , 1 | SemicolonSep      , 363 , 240 , // 363  - gsw-li
            0x47   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 365 , 143 , // 364  - gu
            0x447  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 365 , 143 , // 365  - gu-in
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 367 , 240 , // 366  - guz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 367 , 240 , // 367  - guz-ke
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3b16, 1 | SemicolonSep      , 369 , 240 , // 368  - gv
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3b16, 1 | SemicolonSep      , 369 , 240 , // 369  - gv-im
            0x68   , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xaf  , 1 | SemicolonSep      , 374 , 374 , // 370  - ha
            0x7c68 , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xaf  , 1 | SemicolonSep      , 374 , 374 , // 371  - ha-latn
            0x1000 , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0x59  , 1 | SemicolonSep      , 372 , 240 , // 372  - ha-latn-gh
            0x1000 , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0xad  , 1 | SemicolonSep      , 373 , 240 , // 373  - ha-latn-ne
            0x468  , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xaf  , 1 | SemicolonSep      , 374 , 374 , // 374  - ha-latn-ng
            0x75   , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xf4  , 1 | SemicolonSep      , 376 , 376 , // 375  - haw
            0x475  , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xf4  , 1 | SemicolonSep      , 376 , 376 , // 376  - haw-us
            0xd    , 0x4e7 , 0x35e , 0x2715, 0x1f4 , 0x75  , 1 | CommaSep          , 378 , 143 , // 377  - he
            0x40d  , 0x4e7 , 0x35e , 0x2715, 0x1f4 , 0x75  , 1 | CommaSep          , 378 , 143 , // 378  - he-il
            0x39   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 380 , 143 , // 379  - hi
            0x439  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 380 , 143 , // 380  - hi-in
            0x1a   , 0x4e2 , 0x354 , 0x2762, 0x1f4 , 0x6c  , 1 | SemicolonSep      , 383 , 383 , // 381  - hr
            0x101a , 0x4e2 , 0x354 , 0x2762, 0x366 , 0x19  , 1 | SemicolonSep      , 382 , 382 , // 382  - hr-ba
            0x41a  , 0x4e2 , 0x354 , 0x2762, 0x1f4 , 0x6c  , 1 | SemicolonSep      , 383 , 383 , // 383  - hr-hr
            0x2e   , 0x4e4 , 0x352 , 0x2710, 0x366 , 0x5e  , 1 | SemicolonSep      , 385 , 385 , // 384  - hsb
            0x42e  , 0x4e4 , 0x352 , 0x2710, 0x366 , 0x5e  , 1 | SemicolonSep      , 385 , 385 , // 385  - hsb-de
            0xe    , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x6d  , 1 | SemicolonSep      , 387 , 387 , // 386  - hu
            0x40e  , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x6d  , 1 | SemicolonSep      , 387 , 387 , // 387  - hu-hu
            0x1040e, 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x6d  , 1 | SemicolonSep      , 387 , 387 , // 388  - hu-hu_technl
            0x2b   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x7   , 1 | CommaSep          , 390 , 390 , // 389  - hy
            0x42b  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x7   , 1 | CommaSep          , 390 , 390 , // 390  - hy-am
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x54  , 1 | SemicolonSep      , 393 , 240 , // 391  - ia
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x989e, 1 | SemicolonSep      , 392 , 240 , // 392  - ia-001
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x54  , 1 | SemicolonSep      , 393 , 240 , // 393  - ia-fr
            0x69   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xaf  , 1 | SemicolonSep      , 395 , 240 , // 394  - ibb
            0x469  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xaf  , 1 | SemicolonSep      , 395 , 240 , // 395  - ibb-ng
            0x21   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x6f  , 1 | SemicolonSep      , 397 , 397 , // 396  - id
            0x421  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x6f  , 1 | SemicolonSep      , 397 , 397 , // 397  - id-id
            0x70   , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xaf  , 1 | SemicolonSep      , 399 , 399 , // 398  - ig
            0x470  , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xaf  , 1 | SemicolonSep      , 399 , 399 , // 399  - ig-ng
            0x78   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2d  , 1 | SemicolonSep      , 401 , 143 , // 400  - ii
            0x478  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2d  , 1 | SemicolonSep      , 401 , 143 , // 401  - ii-cn
            0xf    , 0x4e4 , 0x352 , 0x275f, 0x5187, 0x6e  , 1 | SemicolonSep      , 403 , 403 , // 402  - is
            0x40f  , 0x4e4 , 0x352 , 0x275f, 0x5187, 0x6e  , 1 | SemicolonSep      , 403 , 403 , // 403  - is-is
            0x10   , 0x4e4 , 0x352 , 0x2710, 0x4f38, 0x76  , 1 | SemicolonSep      , 406 , 406 , // 404  - it
            0x810  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xdf  , 1 | SemicolonSep      , 405 , 405 , // 405  - it-ch
            0x410  , 0x4e4 , 0x352 , 0x2710, 0x4f38, 0x76  , 1 | SemicolonSep      , 406 , 406 , // 406  - it-it
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f38, 0xd6  , 1 | SemicolonSep      , 407 , 240 , // 407  - it-sm
            0x5d   , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0x27  , 1 | CommaSep          , 412 , 412 , // 408  - iu
            0x785d , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x27  , 1 | CommaSep          , 410 , 143 , // 409  - iu-cans
            0x45d  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x27  , 1 | CommaSep          , 410 , 143 , // 410  - iu-cans-ca
            0x7c5d , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0x27  , 1 | CommaSep          , 412 , 412 , // 411  - iu-latn
            0x85d  , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0x27  , 1 | CommaSep          , 412 , 412 , // 412  - iu-latn-ca
            0x11   , 0x3a4 , 0x3a4 , 0x2711, 0x4f42, 0x7a  , 1 | CommaSep          , 414 , 414 , // 413  - ja
            0x411  , 0x3a4 , 0x3a4 , 0x2711, 0x4f42, 0x7a  , 1 | CommaSep          , 414 , 414 , // 414  - ja-jp
            0x40411, 0x3a4 , 0x3a4 , 0x2711, 0x4f42, 0x7a  , 1 | CommaSep          , 414 , 414 , // 415  - ja-jp_radstr
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 417 , 240 , // 416  - jgo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 417 , 240 , // 417  - jgo-cm
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 419 , 240 , // 418  - jmc
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 419 , 240 , // 419  - jmc-tz
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x6f  , 1 | SemicolonSep      , 424 , 424 , // 420  - jv
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x6f  , 1 | SemicolonSep      , 422 , 424 , // 421  - jv-java
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x6f  , 1 | SemicolonSep      , 422 , 424 , // 422  - jv-java-id
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x6f  , 1 | SemicolonSep      , 424 , 424 , // 423  - jv-latn
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x6f  , 1 | SemicolonSep      , 424 , 424 , // 424  - jv-latn-id
            0x37   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x58  , 1 | SemicolonSep      , 426 , 426 , // 425  - ka
            0x437  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x58  , 1 | SemicolonSep      , 426 , 426 , // 426  - ka-ge
            0x10437, 0x0   , 0x1   , 0x2   , 0x1f4 , 0x58  , 1 | SemicolonSep      , 426 , 426 , // 427  - ka-ge_modern
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x4   , 1 | SemicolonSep      , 429 , 240 , // 428  - kab
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x4   , 1 | SemicolonSep      , 429 , 240 , // 429  - kab-dz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 431 , 240 , // 430  - kam
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 431 , 240 , // 431  - kam-ke
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 433 , 240 , // 432  - kde
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 433 , 240 , // 433  - kde-tz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x39  , 1 | SemicolonSep      , 435 , 240 , // 434  - kea
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x39  , 1 | SemicolonSep      , 435 , 240 , // 435  - kea-cv
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9d  , 1 | SemicolonSep      , 437 , 240 , // 436  - khq
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9d  , 1 | SemicolonSep      , 437 , 240 , // 437  - khq-ml
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 439 , 240 , // 438  - ki
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 439 , 240 , // 439  - ki-ke
            0x3f   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x89  , 1 | SemicolonSep      , 441 , 441 , // 440  - kk
            0x43f  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x89  , 1 | SemicolonSep      , 441 , 441 , // 441  - kk-kz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 443 , 240 , // 442  - kkj
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 443 , 240 , // 443  - kkj-cm
            0x6f   , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0x5d  , 1 | SemicolonSep      , 445 , 445 , // 444  - kl
            0x46f  , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0x5d  , 1 | SemicolonSep      , 445 , 445 , // 445  - kl-gl
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 447 , 240 , // 446  - kln
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 447 , 240 , // 447  - kln-ke
            0x53   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x28  , 2 | CommaSep          , 449 , 143 , // 448  - km
            0x453  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x28  , 2 | CommaSep          , 449 , 143 , // 449  - km-kh
            0x4b   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 451 , 143 , // 450  - kn
            0x44b  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 451 , 143 , // 451  - kn-in
            0x12   , 0x3b5 , 0x3b5 , 0x2713, 0x5161, 0x86  , 1 | CommaSep          , 454 , 454 , // 452  - ko
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x83  , 1 | SemicolonSep      , 453 , 240 , // 453  - ko-kp
            0x412  , 0x3b5 , 0x3b5 , 0x2713, 0x5161, 0x86  , 1 | CommaSep          , 454 , 454 , // 454  - ko-kr
            0x57   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 456 , 143 , // 455  - kok
            0x457  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 456 , 143 , // 456  - kok-in
            0x71   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xaf  , 1 | SemicolonSep      , 458 , 240 , // 457  - kr
            0x471  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xaf  , 1 | SemicolonSep      , 458 , 240 , // 458  - kr-ng
            0x60   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 2 | SemicolonSep      , 461 , 240 , // 459  - ks
            0x460  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 2 | SemicolonSep      , 461 , 240 , // 460  - ks-arab
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 2 | SemicolonSep      , 461 , 240 , // 461  - ks-arab-in
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 463 , 187 , // 462  - ks-deva
            0x860  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 463 , 187 , // 463  - ks-deva-in
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 465 , 240 , // 464  - ksb
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 465 , 240 , // 465  - ksb-tz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 467 , 240 , // 466  - ksf
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 467 , 240 , // 467  - ksf-cm
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x5e  , 1 | SemicolonSep      , 469 , 240 , // 468  - ksh
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x5e  , 1 | SemicolonSep      , 469 , 240 , // 469  - ksh-de
            0x92   , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x79  , 0 | ArabicSemicolonSep, 472 , 143 , // 470  - ku
            0x7c92 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x79  , 0 | ArabicSemicolonSep, 472 , 143 , // 471  - ku-arab
            0x492  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x79  , 0 | ArabicSemicolonSep, 472 , 143 , // 472  - ku-arab-iq
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x74  , 0 | SemicolonSep      , 473 , 240 , // 473  - ku-arab-ir
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf2  , 1 | SemicolonSep      , 475 , 240 , // 474  - kw
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf2  , 1 | SemicolonSep      , 475 , 240 , // 475  - kw-gb
            0x40   , 0x4e3 , 0x362 , 0x2717, 0x5190, 0x82  , 1 | SemicolonSep      , 477 , 477 , // 476  - ky
            0x440  , 0x4e3 , 0x362 , 0x2717, 0x5190, 0x82  , 1 | SemicolonSep      , 477 , 477 , // 477  - ky-kg
            0x76   , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0x989e, 1 | CommaSep          , 479 , 143 , // 478  - la
            0x476  , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0x989e, 1 | CommaSep          , 479 , 143 , // 479  - la-001
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 481 , 240 , // 480  - lag
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 481 , 240 , // 481  - lag-tz
            0x6e   , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x93  , 1 | SemicolonSep      , 483 , 483 , // 482  - lb
            0x46e  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x93  , 1 | SemicolonSep      , 483 , 483 , // 483  - lb-lu
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 485 , 240 , // 484  - lg
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 485 , 240 , // 485  - lg-ug
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf4  , 1 | SemicolonSep      , 487 , 240 , // 486  - lkt
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf4  , 1 | SemicolonSep      , 487 , 240 , // 487  - lkt-us
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2c  , 1 | SemicolonSep      , 490 , 240 , // 488  - ln
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9   , 1 | SemicolonSep      , 489 , 240 , // 489  - ln-ao
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2c  , 1 | SemicolonSep      , 490 , 240 , // 490  - ln-cd
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x37  , 1 | SemicolonSep      , 491 , 240 , // 491  - ln-cf
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2b  , 1 | SemicolonSep      , 492 , 240 , // 492  - ln-cg
            0x54   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x8a  , 1 | SemicolonSep      , 494 , 143 , // 493  - lo
            0x454  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x8a  , 1 | SemicolonSep      , 494 , 143 , // 494  - lo-la
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x74  , 2 | SemicolonSep      , 497 , 240 , // 495  - lrc
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x79  , 2 | SemicolonSep      , 496 , 240 , // 496  - lrc-iq
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x74  , 2 | SemicolonSep      , 497 , 240 , // 497  - lrc-ir
            0x27   , 0x4e9 , 0x307 , 0x272d, 0x1f4 , 0x8d  , 1 | SemicolonSep      , 499 , 499 , // 498  - lt
            0x427  , 0x4e9 , 0x307 , 0x272d, 0x1f4 , 0x8d  , 1 | SemicolonSep      , 499 , 499 , // 499  - lt-lt
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2c  , 1 | SemicolonSep      , 501 , 240 , // 500  - lu
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2c  , 1 | SemicolonSep      , 501 , 240 , // 501  - lu-cd
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 503 , 240 , // 502  - luo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 503 , 240 , // 503  - luo-ke
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 505 , 240 , // 504  - luy
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 505 , 240 , // 505  - luy-ke
            0x26   , 0x4e9 , 0x307 , 0x272d, 0x1f4 , 0x8c  , 1 | SemicolonSep      , 507 , 507 , // 506  - lv
            0x426  , 0x4e9 , 0x307 , 0x272d, 0x1f4 , 0x8c  , 1 | SemicolonSep      , 507 , 507 , // 507  - lv-lv
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 509 , 240 , // 508  - mas
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 509 , 240 , // 509  - mas-ke
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 510 , 240 , // 510  - mas-tz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 512 , 240 , // 511  - mer
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 512 , 240 , // 512  - mer-ke
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa0  , 1 | SemicolonSep      , 514 , 240 , // 513  - mfe
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa0  , 1 | SemicolonSep      , 514 , 240 , // 514  - mfe-mu
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x95  , 1 | SemicolonSep      , 516 , 240 , // 515  - mg
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x95  , 1 | SemicolonSep      , 516 , 240 , // 516  - mg-mg
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa8  , 1 | SemicolonSep      , 518 , 240 , // 517  - mgh
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa8  , 1 | SemicolonSep      , 518 , 240 , // 518  - mgh-mz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 520 , 240 , // 519  - mgo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 520 , 240 , // 520  - mgo-cm
            0x81   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xb7  , 1 | CommaSep          , 522 , 522 , // 521  - mi
            0x481  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xb7  , 1 | CommaSep          , 522 , 522 , // 522  - mi-nz
            0x2f   , 0x4e3 , 0x362 , 0x2717, 0x1f4 , 0x4ca2, 1 | SemicolonSep      , 524 , 524 , // 523  - mk
            0x42f  , 0x4e3 , 0x362 , 0x2717, 0x1f4 , 0x4ca2, 1 | SemicolonSep      , 524 , 524 , // 524  - mk-mk
            0x4c   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | SemicolonSep      , 526 , 143 , // 525  - ml
            0x44c  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | SemicolonSep      , 526 , 143 , // 526  - ml-in
            0x50   , 0x4e3 , 0x362 , 0x2717, 0x5190, 0x9a  , 1 | SemicolonSep      , 529 , 529 , // 527  - mn
            0x7850 , 0x4e3 , 0x362 , 0x2717, 0x5190, 0x9a  , 1 | SemicolonSep      , 529 , 529 , // 528  - mn-cyrl
            0x450  , 0x4e3 , 0x362 , 0x2717, 0x5190, 0x9a  , 1 | SemicolonSep      , 529 , 529 , // 529  - mn-mn
            0x7c50 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2d  , 1 | CommaSep          , 531 , 531 , // 530  - mn-mong
            0x850  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2d  , 1 | CommaSep          , 531 , 531 , // 531  - mn-mong-cn
            0xc50  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9a  , 1 | CommaSep          , 532 , 532 , // 532  - mn-mong-mn
            0x58   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 534 , 187 , // 533  - mni
            0x458  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 534 , 187 , // 534  - mni-in
            0x7c   , 0x4e4 , 0x352 , 0x2710, 0x25  , 0x27  , 1 | CommaSep          , 536 , 240 , // 535  - moh
            0x47c  , 0x4e4 , 0x352 , 0x2710, 0x25  , 0x27  , 1 | CommaSep          , 536 , 240 , // 536  - moh-ca
            0x4e   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 538 , 143 , // 537  - mr
            0x44e  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 538 , 143 , // 538  - mr-in
            0x3e   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xa7  , 1 | SemicolonSep      , 541 , 541 , // 539  - ms
            0x83e  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x25  , 1 | SemicolonSep      , 540 , 540 , // 540  - ms-bn
            0x43e  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xa7  , 1 | SemicolonSep      , 541 , 541 , // 541  - ms-my
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd7  , 1 | SemicolonSep      , 542 , 240 , // 542  - ms-sg
            0x3a   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa3  , 1 | SemicolonSep      , 544 , 544 , // 543  - mt
            0x43a  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa3  , 1 | SemicolonSep      , 544 , 544 , // 544  - mt-mt
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 546 , 240 , // 545  - mua
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 546 , 240 , // 546  - mua-cm
            0x55   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x1b  , 2 | SemicolonSep      , 548 , 240 , // 547  - my
            0x455  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x1b  , 2 | SemicolonSep      , 548 , 240 , // 548  - my-mm
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x74  , 2 | SemicolonSep      , 550 , 240 , // 549  - mzn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x74  , 2 | SemicolonSep      , 550 , 240 , // 550  - mzn-ir
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xfe  , 1 | SemicolonSep      , 552 , 240 , // 551  - naq
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xfe  , 1 | SemicolonSep      , 552 , 240 , // 552  - naq-na
            0x7c14 , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xb1  , 1 | SemicolonSep      , 554 , 554 , // 553  - nb
            0x414  , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xb1  , 1 | SemicolonSep      , 554 , 554 , // 554  - nb-no
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xdc  , 1 | SemicolonSep      , 555 , 240 , // 555  - nb-sj
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x108 , 1 | SemicolonSep      , 557 , 240 , // 556  - nd
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x108 , 1 | SemicolonSep      , 557 , 240 , // 557  - nd-zw
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x5e  , 1 | SemicolonSep      , 559 , 240 , // 558  - nds
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x5e  , 1 | SemicolonSep      , 559 , 240 , // 559  - nds-de
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xb0  , 1 | SemicolonSep      , 560 , 240 , // 560  - nds-nl
            0x61   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xb2  , 1 | CommaSep          , 563 , 143 , // 561  - ne
            0x861  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 2 | SemicolonSep      , 562 , 240 , // 562  - ne-in
            0x461  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xb2  , 1 | CommaSep          , 563 , 143 , // 563  - ne-np
            0x13   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xb0  , 1 | SemicolonSep      , 569 , 569 , // 564  - nl
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x12e , 1 | SemicolonSep      , 565 , 240 , // 565  - nl-aw
            0x813  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x15  , 1 | SemicolonSep      , 566 , 566 , // 566  - nl-be
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x9a55d42, 1 | SemicolonSep   , 567 , 240 , // 567  - nl-bq
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x111 , 1 | SemicolonSep      , 568 , 240 , // 568  - nl-cw
            0x413  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xb0  , 1 | SemicolonSep      , 569 , 569 , // 569  - nl-nl
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xb5  , 1 | SemicolonSep      , 570 , 240 , // 570  - nl-sr
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x78f7, 1 | SemicolonSep      , 571 , 240 , // 571  - nl-sx
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 573 , 240 , // 572  - nmg
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 573 , 240 , // 573  - nmg-cm
            0x7814 , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xb1  , 1 | SemicolonSep      , 575 , 575 , // 574  - nn
            0x814  , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xb1  , 1 | SemicolonSep      , 575 , 575 , // 575  - nn-no
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 577 , 240 , // 576  - nnh
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 577 , 240 , // 577  - nnh-cm
            0x14   , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xb1  , 1 | SemicolonSep      , 554 , 554 , // 578  - no
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x64  , 2 | ArabicCommaSep    , 580 , 143 , // 579  - nqo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x64  , 2 | ArabicCommaSep    , 580 , 143 , // 580  - nqo-gn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 582 , 240 , // 581  - nr
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 582 , 240 , // 582  - nr-za
            0x6c   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 584 , 584 , // 583  - nso
            0x46c  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 584 , 584 , // 584  - nso-za
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x114 , 1 | SemicolonSep      , 586 , 240 , // 585  - nus
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x114 , 1 | SemicolonSep      , 586 , 240 , // 586  - nus-ss
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 588 , 240 , // 587  - nyn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 588 , 240 , // 588  - nyn-ug
            0x82   , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x54  , 1 | SemicolonSep      , 590 , 590 , // 589  - oc
            0x482  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x54  , 1 | SemicolonSep      , 590 , 590 , // 590  - oc-fr
            0x72   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 592 , 240 , // 591  - om
            0x472  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 592 , 240 , // 592  - om-et
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 593 , 240 , // 593  - om-ke
            0x48   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 595 , 143 , // 594  - or
            0x448  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 595 , 143 , // 595  - or-in
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x58  , 1 | SemicolonSep      , 597 , 240 , // 596  - os
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x58  , 1 | SemicolonSep      , 597 , 240 , // 597  - os-ge
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xcb  , 1 | SemicolonSep      , 598 , 240 , // 598  - os-ru
            0x46   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 602 , 143 , // 599  - pa
            0x7c46 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xbe  , 2 | SemicolonSep      , 601 , 143 , // 600  - pa-arab
            0x846  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xbe  , 2 | SemicolonSep      , 601 , 143 , // 601  - pa-arab-pk
            0x446  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 602 , 143 , // 602  - pa-in
            0x79   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x993248, 1 | CommaSep        , 604 , 145 , // 603  - pap
            0x479  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x993248, 1 | CommaSep        , 604 , 145 , // 604  - pap-029
            0x15   , 0x4e2 , 0x354 , 0x272d, 0x5190, 0xbf  , 1 | SemicolonSep      , 606 , 606 , // 605  - pl
            0x415  , 0x4e2 , 0x354 , 0x272d, 0x5190, 0xbf  , 1 | SemicolonSep      , 606 , 606 , // 606  - pl-pl
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x989e, 1 | SemicolonSep      , 608 , 240 , // 607  - prg
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x989e, 1 | SemicolonSep      , 608 , 240 , // 608  - prg-001
            0x8c   , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x3   , 2 | SemicolonSep      , 610 , 143 , // 609  - prs
            0x48c  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x3   , 2 | SemicolonSep      , 610 , 143 , // 610  - prs-af
            0x63   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3   , 2 | SemicolonSep      , 612 , 143 , // 611  - ps
            0x463  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3   , 2 | SemicolonSep      , 612 , 143 , // 612  - ps-af
            0x16   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x20  , 1 | SemicolonSep      , 615 , 615 , // 613  - pt
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x9   , 1 | SemicolonSep      , 614 , 240 , // 614  - pt-ao
            0x416  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x20  , 1 | SemicolonSep      , 615 , 615 , // 615  - pt-br
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xdf  , 1 | SemicolonSep      , 616 , 240 , // 616  - pt-ch
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x39  , 1 | SemicolonSep      , 617 , 240 , // 617  - pt-cv
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x45  , 1 | SemicolonSep      , 618 , 240 , // 618  - pt-gq
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xc4  , 1 | SemicolonSep      , 619 , 240 , // 619  - pt-gw
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x93  , 1 | SemicolonSep      , 620 , 240 , // 620  - pt-lu
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x97  , 1 | SemicolonSep      , 621 , 240 , // 621  - pt-mo
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xa8  , 1 | SemicolonSep      , 622 , 240 , // 622  - pt-mz
            0x816  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xc1  , 1 | SemicolonSep      , 623 , 623 , // 623  - pt-pt
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xe9  , 1 | SemicolonSep      , 624 , 240 , // 624  - pt-st
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x6f60e7,1| SemicolonSep      , 625 , 240 , // 625  - pt-tl
            0x901  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x7c  , 1 | CommaSep          , 626 , 190 , // 626  - qps-latn-x-sh
            0x501  , 0x4e2 , 0x354 , 0x272d, 0x5190, 0xf4  , 1 | DoubleCommaSep    , 627 , 627 , // 627  - qps-ploc
            0x5fe  , 0x3a4 , 0x3a4 , 0x2711, 0x4f42, 0x7a  , 1 | CommaSep          , 628 , 628 , // 628  - qps-ploca
            0x9ff  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xcd  , 0 | SemicolonSep      , 629 , 143 , // 629  - qps-plocm
            0x86   , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x63  , 1 | CommaSep          , 632 , 632 , // 630  - quc
            0x7c86 , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x63  , 1 | CommaSep          , 632 , 632 , // 631  - quc-latn
            0x486  , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x63  , 1 | CommaSep          , 632 , 632 , // 632  - quc-latn-gt
            0x6b   , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x1a  , 1 | CommaSep          , 634 , 634 , // 633  - quz
            0x46b  , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x1a  , 1 | CommaSep          , 634 , 634 , // 634  - quz-bo
            0x86b  , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x42  , 1 | CommaSep          , 635 , 635 , // 635  - quz-ec
            0xc6b  , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xbb  , 1 | CommaSep          , 636 , 636 , // 636  - quz-pe
            0x17   , 0x4e4 , 0x352 , 0x2710, 0x4f31, 0xdf  , 1 | SemicolonSep      , 638 , 638 , // 637  - rm
            0x417  , 0x4e4 , 0x352 , 0x2710, 0x4f31, 0xdf  , 1 | SemicolonSep      , 638 , 638 , // 638  - rm-ch
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x26  , 1 | SemicolonSep      , 640 , 240 , // 639  - rn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x26  , 1 | SemicolonSep      , 640 , 240 , // 640  - rn-bi
            0x18   , 0x4e2 , 0x354 , 0x272d, 0x5190, 0xc8  , 1 | SemicolonSep      , 643 , 643 , // 641  - ro
            0x818  , 0x4e2 , 0x354 , 0x2   , 0x1f4 , 0x98  , 1 | SemicolonSep      , 642 , 240 , // 642  - ro-md
            0x418  , 0x4e2 , 0x354 , 0x272d, 0x5190, 0xc8  , 1 | SemicolonSep      , 643 , 643 , // 643  - ro-ro
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 645 , 240 , // 644  - rof
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 645 , 240 , // 645  - rof-tz
            0x19   , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xcb  , 1 | SemicolonSep      , 651 , 651 , // 646  - ru
            0x1000 , 0x4e3 , 0x362 , 0x2   , 0x1f4 , 0x1d  , 1 | SemicolonSep      , 647 , 240 , // 647  - ru-by
            0x1000 , 0x4e3 , 0x362 , 0x2   , 0x1f4 , 0x82  , 1 | SemicolonSep      , 648 , 240 , // 648  - ru-kg
            0x1000 , 0x4e3 , 0x362 , 0x2   , 0x1f4 , 0x89  , 1 | SemicolonSep      , 649 , 240 , // 649  - ru-kz
            0x819  , 0x4e3 , 0x362 , 0x2   , 0x1f4 , 0x98  , 1 | SemicolonSep      , 650 , 240 , // 650  - ru-md
            0x419  , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xcb  , 1 | SemicolonSep      , 651 , 651 , // 651  - ru-ru
            0x1000 , 0x4e3 , 0x362 , 0x2   , 0x1f4 , 0xf1  , 1 | SemicolonSep      , 652 , 240 , // 652  - ru-ua
            0x87   , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xcc  , 1 | SemicolonSep      , 654 , 654 , // 653  - rw
            0x487  , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xcc  , 1 | SemicolonSep      , 654 , 654 , // 654  - rw-rw
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 656 , 240 , // 655  - rwk
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 656 , 240 , // 656  - rwk-tz
            0x4f   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 658 , 143 , // 657  - sa
            0x44f  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 658 , 143 , // 658  - sa-in
            0x85   , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xcb  , 1 | SemicolonSep      , 660 , 660 , // 659  - sah
            0x485  , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xcb  , 1 | SemicolonSep      , 660 , 660 , // 660  - sah-ru
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 662 , 240 , // 661  - saq
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 662 , 240 , // 662  - saq-ke
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 664 , 240 , // 663  - sbp
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 664 , 240 , // 664  - sbp-tz
            0x59   , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xbe  , 2 | SemicolonSep      , 667 , 143 , // 665  - sd
            0x7c59 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xbe  , 2 | SemicolonSep      , 667 , 143 , // 666  - sd-arab
            0x859  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xbe  , 2 | SemicolonSep      , 667 , 143 , // 667  - sd-arab-pk
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 669 , 187 , // 668  - sd-deva
            0x459  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 669 , 187 , // 669  - sd-deva-in
            0x3b   , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xb1  , 1 | SemicolonSep      , 672 , 672 , // 670  - se
            0xc3b  , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0x4d  , 1 | SemicolonSep      , 671 , 671 , // 671  - se-fi
            0x43b  , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xb1  , 1 | SemicolonSep      , 672 , 672 , // 672  - se-no
            0x83b  , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0xdd  , 1 | SemicolonSep      , 673 , 673 , // 673  - se-se
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa8  , 1 | SemicolonSep      , 675 , 240 , // 674  - seh
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa8  , 1 | SemicolonSep      , 675 , 240 , // 675  - seh-mz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9d  , 1 | SemicolonSep      , 677 , 240 , // 676  - ses
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9d  , 1 | SemicolonSep      , 677 , 240 , // 677  - ses-ml
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x37  , 1 | SemicolonSep      , 679 , 240 , // 678  - sg
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x37  , 1 | SemicolonSep      , 679 , 240 , // 679  - sg-cf
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 684 , 240 , // 680  - shi
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 682 , 240 , // 681  - shi-latn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 682 , 240 , // 682  - shi-latn-ma
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 684 , 240 , // 683  - shi-tfng
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 684 , 240 , // 684  - shi-tfng-ma
            0x5b   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2a  , 1 | SemicolonSep      , 686 , 143 , // 685  - si
            0x45b  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2a  , 1 | SemicolonSep      , 686 , 143 , // 686  - si-lk
            0x1b   , 0x4e2 , 0x354 , 0x272d, 0x5190, 0x8f  , 1 | SemicolonSep      , 688 , 688 , // 687  - sk
            0x41b  , 0x4e2 , 0x354 , 0x272d, 0x5190, 0x8f  , 1 | SemicolonSep      , 688 , 688 , // 688  - sk-sk
            0x24   , 0x4e2 , 0x354 , 0x272d, 0x5190, 0xd4  , 1 | SemicolonSep      , 690 , 690 , // 689  - sl
            0x424  , 0x4e2 , 0x354 , 0x272d, 0x5190, 0xd4  , 1 | SemicolonSep      , 690 , 690 , // 690  - sl-si
            0x783b , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0xdd  , 1 | SemicolonSep      , 693 , 693 , // 691  - sma
            0x183b , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xb1  , 1 | SemicolonSep      , 692 , 692 , // 692  - sma-no
            0x1c3b , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0xdd  , 1 | SemicolonSep      , 693 , 693 , // 693  - sma-se
            0x7c3b , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0xdd  , 1 | SemicolonSep      , 696 , 696 , // 694  - smj
            0x103b , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xb1  , 1 | SemicolonSep      , 695 , 695 , // 695  - smj-no
            0x143b , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0xdd  , 1 | SemicolonSep      , 696 , 696 , // 696  - smj-se
            0x703b , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0x4d  , 1 | SemicolonSep      , 698 , 698 , // 697  - smn
            0x243b , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0x4d  , 1 | SemicolonSep      , 698 , 698 , // 698  - smn-fi
            0x743b , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0x4d  , 1 | SemicolonSep      , 700 , 700 , // 699  - sms
            0x203b , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0x4d  , 1 | SemicolonSep      , 700 , 700 , // 700  - sms-fi
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x108 , 1 | SemicolonSep      , 703 , 240 , // 701  - sn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x108 , 1 | SemicolonSep      , 703 , 240 , // 702  - sn-latn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x108 , 1 | SemicolonSep      , 703 , 240 , // 703  - sn-latn-zw
            0x77   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd8  , 1 | SemicolonSep      , 708 , 240 , // 704  - so
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3e  , 1 | SemicolonSep      , 705 , 240 , // 705  - so-dj
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 706 , 240 , // 706  - so-et
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 707 , 240 , // 707  - so-ke
            0x477  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd8  , 1 | SemicolonSep      , 708 , 240 , // 708  - so-so
            0x1c   , 0x4e2 , 0x354 , 0x272d, 0x5190, 0x6   , 1 | SemicolonSep      , 710 , 710 , // 709  - sq
            0x41c  , 0x4e2 , 0x354 , 0x272d, 0x5190, 0x6   , 1 | SemicolonSep      , 710 , 710 , // 710  - sq-al
            0x1000 , 0x4e2 , 0x354 , 0x272d, 0x5190, 0x4ca2, 1 | SemicolonSep      , 711 , 240 , // 711  - sq-mk
            0x1000 , 0x4e2 , 0x354 , 0x272d, 0x5190, 0x974941, 1 | SemicolonSep    , 712 , 240 , // 712  - sq-xk
            0x7c1a , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x10f , 1 | SemicolonSep      , 724 , 724 , // 713  - sr
            0x6c1a , 0x4e3 , 0x357 , 0x2717, 0x5221, 0x10f , 1 | SemicolonSep      , 718 , 718 , // 714  - sr-cyrl
            0x1c1a , 0x4e3 , 0x357 , 0x2717, 0x5221, 0x19  , 1 | SemicolonSep      , 715 , 715 , // 715  - sr-cyrl-ba
            0xc1a  , 0x4e3 , 0x357 , 0x2717, 0x5221, 0x10d , 1 | SemicolonSep      , 716 , 716 , // 716  - sr-cyrl-cs
            0x301a , 0x4e3 , 0x357 , 0x2717, 0x5221, 0x10e , 1 | SemicolonSep      , 717 , 717 , // 717  - sr-cyrl-me
            0x281a , 0x4e3 , 0x357 , 0x2717, 0x5221, 0x10f , 1 | SemicolonSep      , 718 , 718 , // 718  - sr-cyrl-rs
            0x1000 , 0x4e3 , 0x357 , 0x2717, 0x5221, 0x974941, 1 | SemicolonSep    , 719 , 240 , // 719  - sr-cyrl-xk
            0x701a , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x10f , 1 | SemicolonSep      , 724 , 724 , // 720  - sr-latn
            0x181a , 0x4e2 , 0x354 , 0x2762, 0x366 , 0x19  , 1 | SemicolonSep      , 721 , 721 , // 721  - sr-latn-ba
            0x81a  , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x10d , 1 | SemicolonSep      , 722 , 722 , // 722  - sr-latn-cs
            0x2c1a , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x10e , 1 | SemicolonSep      , 723 , 723 , // 723  - sr-latn-me
            0x241a , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x10f , 1 | SemicolonSep      , 724 , 724 , // 724  - sr-latn-rs
            0x1000 , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x974941, 1 | SemicolonSep    , 725 , 240 , // 725  - sr-latn-xk
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 728 , 240 , // 726  - ss
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x104 , 1 | SemicolonSep      , 727 , 240 , // 727  - ss-sz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 728 , 240 , // 728  - ss-za
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x47  , 1 | SemicolonSep      , 730 , 240 , // 729  - ssy
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x47  , 1 | SemicolonSep      , 730 , 240 , // 730  - ssy-er
            0x30   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 733 , 240 , // 731  - st
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x92  , 1 | SemicolonSep      , 732 , 240 , // 732  - st-ls
            0x430  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 733 , 240 , // 733  - st-za
            0x1d   , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0xdd  , 1 | SemicolonSep      , 737 , 737 , // 734  - sv
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0x9906f5, 1 | SemicolonSep    , 735 , 240 , // 735  - sv-ax
            0x81d  , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0x4d  , 1 | SemicolonSep      , 736 , 736 , // 736  - sv-fi
            0x41d  , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0xdd  , 1 | SemicolonSep      , 737 , 737 , // 737  - sv-se
            0x41   , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0x81  , 1 | SemicolonSep      , 740 , 740 , // 738  - sw
            0x1000 , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0x2c  , 1 | SemicolonSep      , 739 , 740 , // 739  - sw-cd
            0x441  , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0x81  , 1 | SemicolonSep      , 740 , 740 , // 740  - sw-ke
            0x1000 , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0xef  , 1 | SemicolonSep      , 741 , 240 , // 741  - sw-tz
            0x1000 , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0xf0  , 1 | SemicolonSep      , 742 , 240 , // 742  - sw-ug
            0x1000 , 0x0   , 0x1   , 0x0   , 0x1f4 , 0x2c  , 1 | CommaSep          , 744 , 240 , // 743  - swc
            0x1000 , 0x0   , 0x1   , 0x0   , 0x1f4 , 0x2c  , 1 | SemicolonSep      , 744 , 240 , // 744  - swc-cd
            0x5a   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xde  , 1 | CommaSep          , 746 , 143 , // 745  - syr
            0x45a  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xde  , 1 | CommaSep          , 746 , 143 , // 746  - syr-sy
            0x49   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 748 , 143 , // 747  - ta
            0x449  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 748 , 143 , // 748  - ta-in
            0x849  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2a  , 1 | SemicolonSep      , 749 , 143 , // 749  - ta-lk
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa7  , 1 | SemicolonSep      , 750 , 240 , // 750  - ta-my
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd7  , 1 | SemicolonSep      , 751 , 240 , // 751  - ta-sg
            0x4a   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | SemicolonSep      , 753 , 143 , // 752  - te
            0x44a  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | SemicolonSep      , 753 , 143 , // 753  - te-in
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 756 , 240 , // 754  - teo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 755 , 240 , // 755  - teo-ke
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 756 , 240 , // 756  - teo-ug
            0x28   , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xe4  , 1 | SemicolonSep      , 759 , 759 , // 757  - tg
            0x7c28 , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xe4  , 1 | SemicolonSep      , 759 , 759 , // 758  - tg-cyrl
            0x428  , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xe4  , 1 | SemicolonSep      , 759 , 759 , // 759  - tg-cyrl-tj
            0x1e   , 0x36a , 0x36a , 0x2725, 0x5166, 0xe3  , 1 | CommaSep          , 761 , 143 , // 760  - th
            0x41e  , 0x36a , 0x36a , 0x2725, 0x5166, 0xe3  , 1 | CommaSep          , 761 , 143 , // 761  - th-th
            0x73   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x47  , 1 | SemicolonSep      , 763 , 143 , // 762  - ti
            0x873  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x47  , 1 | SemicolonSep      , 763 , 143 , // 763  - ti-er
            0x473  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 764 , 143 , // 764  - ti-et
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x47  , 1 | SemicolonSep      , 766 , 240 , // 765  - tig
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x47  , 1 | SemicolonSep      , 766 , 240 , // 766  - tig-er
            0x42   , 0x4e2 , 0x354 , 0x272d, 0x5190, 0xee  , 1 | SemicolonSep      , 768 , 768 , // 767  - tk
            0x442  , 0x4e2 , 0x354 , 0x272d, 0x5190, 0xee  , 1 | SemicolonSep      , 768 , 768 , // 768  - tk-tm
            0x32   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 771 , 771 , // 769  - tn
            0x832  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x13  , 1 | SemicolonSep      , 770 , 770 , // 770  - tn-bw
            0x432  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 771 , 771 , // 771  - tn-za
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xe7  , 1 | SemicolonSep      , 773 , 240 , // 772  - to
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xe7  , 1 | SemicolonSep      , 773 , 240 , // 773  - to-to
            0x1f   , 0x4e6 , 0x359 , 0x2761, 0x51a9, 0xeb  , 1 | SemicolonSep      , 776 , 776 , // 774  - tr
            0x1000 , 0x4e6 , 0x359 , 0x2761, 0x51a9, 0x3b  , 1 | SemicolonSep      , 775 , 240 , // 775  - tr-cy
            0x41f  , 0x4e6 , 0x359 , 0x2761, 0x51a9, 0xeb  , 1 | SemicolonSep      , 776 , 776 , // 776  - tr-tr
            0x31   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 778 , 240 , // 777  - ts
            0x431  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 778 , 240 , // 778  - ts-za
            0x44   , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xcb  , 1 | SemicolonSep      , 780 , 780 , // 779  - tt
            0x444  , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xcb  , 1 | SemicolonSep      , 780 , 780 , // 780  - tt-ru
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xad  , 1 | SemicolonSep      , 782 , 240 , // 781  - twq
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xad  , 1 | SemicolonSep      , 782 , 240 , // 782  - twq-ne
            0x5f   , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x4   , 1 | SemicolonSep      , 787 , 787 , // 783  - tzm
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x9f  , 1 | SemicolonSep      , 785 , 240 , // 784  - tzm-arab
            0x45f  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x9f  , 1 | SemicolonSep      , 785 , 240 , // 785  - tzm-arab-ma
            0x7c5f , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x4   , 1 | SemicolonSep      , 787 , 787 , // 786  - tzm-latn
            0x85f  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x4   , 1 | SemicolonSep      , 787 , 787 , // 787  - tzm-latn-dz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 788 , 240 , // 788  - tzm-latn-ma
            0x785f , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 790 , 316 , // 789  - tzm-tfng
            0x105f , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 790 , 316 , // 790  - tzm-tfng-ma
            0x80   , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x2d  , 1 | CommaSep          , 792 , 143 , // 791  - ug
            0x480  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x2d  , 1 | CommaSep          , 792 , 143 , // 792  - ug-cn
            0x22   , 0x4e3 , 0x362 , 0x2721, 0x1f4 , 0xf1  , 1 | SemicolonSep      , 794 , 794 , // 793  - uk
            0x422  , 0x4e3 , 0x362 , 0x2721, 0x1f4 , 0xf1  , 1 | SemicolonSep      , 794 , 794 , // 794  - uk-ua
            0x20   , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xbe  , 1 | SemicolonSep      , 797 , 143 , // 795  - ur
            0x820  , 0x4e8 , 0x2d0 , 0x2   , 0x1f4 , 0x71  , 2 | SemicolonSep      , 796 , 240 , // 796  - ur-in
            0x420  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xbe  , 1 | SemicolonSep      , 797 , 143 , // 797  - ur-pk
            0x43   , 0x4e6 , 0x359 , 0x272d, 0x1f4 , 0xf7  , 1 | SemicolonSep      , 804 , 804 , // 798  - uz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3   , 2 | SemicolonSep      , 800 , 240 , // 799  - uz-arab
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3   , 2 | SemicolonSep      , 800 , 240 , // 800  - uz-arab-af
            0x7843 , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xf7  , 1 | SemicolonSep      , 802 , 802 , // 801  - uz-cyrl
            0x843  , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xf7  , 1 | SemicolonSep      , 802 , 802 , // 802  - uz-cyrl-uz
            0x7c43 , 0x4e6 , 0x359 , 0x272d, 0x1f4 , 0xf7  , 1 | SemicolonSep      , 804 , 804 , // 803  - uz-latn
            0x443  , 0x4e6 , 0x359 , 0x272d, 0x1f4 , 0xf7  , 1 | SemicolonSep      , 804 , 804 , // 804  - uz-latn-uz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x8e  , 1 | SemicolonSep      , 809 , 240 , // 805  - vai
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x8e  , 1 | SemicolonSep      , 807 , 240 , // 806  - vai-latn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x8e  , 1 | SemicolonSep      , 807 , 240 , // 807  - vai-latn-lr
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x8e  , 1 | SemicolonSep      , 809 , 240 , // 808  - vai-vaii
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x8e  , 1 | SemicolonSep      , 809 , 240 , // 809  - vai-vaii-lr
            0x33   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 811 , 240 , // 810  - ve
            0x433  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 811 , 240 , // 811  - ve-za
            0x2a   , 0x4ea , 0x4ea , 0x2710, 0x1f4 , 0xfb  , 1 | CommaSep          , 813 , 143 , // 812  - vi
            0x42a  , 0x4ea , 0x4ea , 0x2710, 0x1f4 , 0xfb  , 1 | CommaSep          , 813 , 143 , // 813  - vi-vn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x989e, 1 | SemicolonSep      , 815 , 240 , // 814  - vo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x989e, 1 | SemicolonSep      , 815 , 240 , // 815  - vo-001
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 817 , 240 , // 816  - vun
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 817 , 240 , // 817  - vun-tz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xdf  , 1 | SemicolonSep      , 819 , 240 , // 818  - wae
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xdf  , 1 | SemicolonSep      , 819 , 240 , // 819  - wae-ch
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 821 , 240 , // 820  - wal
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 821 , 240 , // 821  - wal-et
            0x88   , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xd2  , 1 | SemicolonSep      , 823 , 823 , // 822  - wo
            0x488  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xd2  , 1 | SemicolonSep      , 823 , 823 , // 823  - wo-sn
            0x1007f, 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xf4  , 1 | CommaSep          , -1  , -1  , // 824  - x-iv_mathan
            0x34   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 826 , 826 , // 825  - xh
            0x434  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 826 , 826 , // 826  - xh-za
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 828 , 240 , // 827  - xog
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 828 , 240 , // 828  - xog-ug
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 830 , 240 , // 829  - yav
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 830 , 240 , // 830  - yav-cm
            0x3d   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x989e, 1 | SemicolonSep      , 832 , 240 , // 831  - yi
            0x43d  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x989e, 1 | SemicolonSep      , 832 , 240 , // 832  - yi-001
            0x6a   , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xaf  , 1 | SemicolonSep      , 835 , 835 , // 833  - yo
            0x1000 , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0x1c  , 1 | SemicolonSep      , 834 , 240 , // 834  - yo-bj
            0x46a  , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xaf  , 1 | SemicolonSep      , 835 , 835 , // 835  - yo-ng
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x68  , 1 | CommaSep          , 837 , 240 , // 836  - yue
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x68  , 1 | CommaSep          , 837 , 240 , // 837  - yue-hk
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 840 , 316 , // 838  - zgh
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 840 , 316 , // 839  - zgh-tfng
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 840 , 316 , // 840  - zgh-tfng-ma
            0x7804 , 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0x2d  , 1 | CommaSep          , 844 , 844 , // 841  - zh
            0x4    , 0x3a8 , 0x3a8 , 0x0   , 0x1f4 , 0x2d  , 1 | CommaSep          , 844 , 844 , // 842  - zh-chs
            0x7c04 , 0x3b6 , 0x3b6 , 0x0   , 0x1f4 , 0x68  , 1 | CommaSep          , 851 , 851 , // 843  - zh-cht
            0x804  , 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0x2d  , 1 | CommaSep          , 844 , 844 , // 844  - zh-cn
            0x50804, 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0x2d  , 1 | CommaSep          , 844 , 844 , // 845  - zh-cn_phoneb
            0x20804, 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0x2d  , 1 | CommaSep          , 844 , 844 , // 846  - zh-cn_stroke
            0x4    , 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0x2d  , 1 | CommaSep          , 844 , 844 , // 847  - zh-hans
            0x1000 , 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0x68  , 1 | SemicolonSep      , 848 , 240 , // 848  - zh-hans-hk
            0x1000 , 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0x97  , 1 | SemicolonSep      , 849 , 240 , // 849  - zh-hans-mo
            0x7c04 , 0x3b6 , 0x3b6 , 0x2712, 0x1f4 , 0x68  , 1 | CommaSep          , 851 , 851 , // 850  - zh-hant
            0xc04  , 0x3b6 , 0x3b6 , 0x2712, 0x1f4 , 0x68  , 1 | CommaSep          , 851 , 851 , // 851  - zh-hk
            0x40c04, 0x3b6 , 0x3b6 , 0x2712, 0x1f4 , 0x68  , 1 | CommaSep          , 851 , 851 , // 852  - zh-hk_radstr
            0x1404 , 0x3b6 , 0x3b6 , 0x2712, 0x1f4 , 0x97  , 1 | CommaSep          , 853 , 853 , // 853  - zh-mo
            0x41404, 0x3b6 , 0x3b6 , 0x2712, 0x1f4 , 0x97  , 1 | CommaSep          , 853 , 853 , // 854  - zh-mo_radstr
            0x21404, 0x3b6 , 0x3b6 , 0x2712, 0x1f4 , 0x97  , 1 | CommaSep          , 853 , 853 , // 855  - zh-mo_stroke
            0x1004 , 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0xd7  , 1 | CommaSep          , 856 , 856 , // 856  - zh-sg
            0x51004, 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0xd7  , 1 | CommaSep          , 856 , 856 , // 857  - zh-sg_phoneb
            0x21004, 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0xd7  , 1 | CommaSep          , 856 , 856 , // 858  - zh-sg_stroke
            0x404  , 0x3b6 , 0x3b6 , 0x2712, 0x1f4 , 0xed  , 1 | CommaSep          , 859 , 859 , // 859  - zh-tw
            0x30404, 0x3b6 , 0x3b6 , 0x2712, 0x1f4 , 0xed  , 1 | CommaSep          , 859 , 859 , // 860  - zh-tw_pronun
            0x40404, 0x3b6 , 0x3b6 , 0x2712, 0x1f4 , 0xed  , 1 | CommaSep          , 859 , 859 , // 861  - zh-tw_radstr
            0x35   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 863 , 863 , // 862  - zu
            0x435  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 863 , 863 , // 863  - zu-za
        };

        static (string, string)[] s_lcids = new (string, string)[]
        {
            ("0x1","ar"), ("0x2","bg"), ("0x3","ca"), ("0x4","zh-chs"), ("0x5","cs"),
            ("0x6","da"), ("0x7","de"), ("0x8","el"), ("0x9","en"), ("0xa","es"),
            ("0xb","fi"), ("0xc","fr"), ("0xd","he"), ("0xe","hu"), ("0xf","is"),
            ("0x10","it"), ("0x11","ja"), ("0x12","ko"), ("0x13","nl"), ("0x14","no"),
            ("0x15","pl"), ("0x16","pt"), ("0x17","rm"), ("0x18","ro"), ("0x19","ru"),
            ("0x1a","hr"), ("0x1b","sk"), ("0x1c","sq"), ("0x1d","sv"), ("0x1e","th"),
            ("0x1f","tr"), ("0x20","ur"), ("0x21","id"), ("0x22","uk"), ("0x23","be"),
            ("0x24","sl"), ("0x25","et"), ("0x26","lv"), ("0x27","lt"), ("0x28","tg"),
            ("0x29","fa"), ("0x2a","vi"), ("0x2b","hy"), ("0x2c","az"), ("0x2d","eu"),
            ("0x2e","hsb"), ("0x2f","mk"), ("0x30","st"), ("0x31","ts"), ("0x32","tn"),
            ("0x33","ve"), ("0x34","xh"), ("0x35","zu"), ("0x36","af"), ("0x37","ka"),
            ("0x38","fo"), ("0x39","hi"), ("0x3a","mt"), ("0x3b","se"), ("0x3c","ga"),
            ("0x3d","yi"), ("0x3e","ms"), ("0x3f","kk"), ("0x40","ky"), ("0x41","sw"),
            ("0x42","tk"), ("0x43","uz"), ("0x44","tt"), ("0x45","bn"), ("0x46","pa"),
            ("0x47","gu"), ("0x48","or"), ("0x49","ta"), ("0x4a","te"), ("0x4b","kn"),
            ("0x4c","ml"), ("0x4d","as"), ("0x4e","mr"), ("0x4f","sa"), ("0x50","mn"),
            ("0x51","bo"), ("0x52","cy"), ("0x53","km"), ("0x54","lo"), ("0x55","my"),
            ("0x56","gl"), ("0x57","kok"), ("0x58","mni"), ("0x59","sd"), ("0x5a","syr"),
            ("0x5b","si"), ("0x5c","chr"), ("0x5d","iu"), ("0x5e","am"), ("0x5f","tzm"),
            ("0x60","ks"), ("0x61","ne"), ("0x62","fy"), ("0x63","ps"), ("0x64","fil"),
            ("0x65","dv"), ("0x66","bin"), ("0x67","ff"), ("0x68","ha"), ("0x69","ibb"),
            ("0x6a","yo"), ("0x6b","quz"), ("0x6c","nso"), ("0x6d","ba"), ("0x6e","lb"),
            ("0x6f","kl"), ("0x70","ig"), ("0x71","kr"), ("0x72","om"), ("0x73","ti"),
            ("0x74","gn"), ("0x75","haw"), ("0x76","la"), ("0x77","so"), ("0x78","ii"),
            ("0x79","pap"), ("0x7a","arn"), ("0x7c","moh"), ("0x7e","br"), ("0x80","ug"),
            ("0x81","mi"), ("0x82","oc"), ("0x83","co"), ("0x84","gsw"), ("0x85","sah"),
            ("0x86","quc"), ("0x87","rw"), ("0x88","wo"), ("0x8c","prs"), ("0x91","gd"),
            ("0x92","ku"), ("0x401","ar-sa"), ("0x402","bg-bg"), ("0x403","ca-es"), ("0x404","zh-tw"),
            ("0x405","cs-cz"), ("0x406","da-dk"), ("0x407","de-de"), ("0x408","el-gr"), ("0x409","en-us"),
            ("0x40a","es-es_tradnl"), ("0x40b","fi-fi"), ("0x40c","fr-fr"), ("0x40d","he-il"), ("0x40e","hu-hu"),
            ("0x40f","is-is"), ("0x410","it-it"), ("0x411","ja-jp"), ("0x412","ko-kr"), ("0x413","nl-nl"),
            ("0x414","nb-no"), ("0x415","pl-pl"), ("0x416","pt-br"), ("0x417","rm-ch"), ("0x418","ro-ro"),
            ("0x419","ru-ru"), ("0x41a","hr-hr"), ("0x41b","sk-sk"), ("0x41c","sq-al"), ("0x41d","sv-se"),
            ("0x41e","th-th"), ("0x41f","tr-tr"), ("0x420","ur-pk"), ("0x421","id-id"), ("0x422","uk-ua"),
            ("0x423","be-by"), ("0x424","sl-si"), ("0x425","et-ee"), ("0x426","lv-lv"), ("0x427","lt-lt"),
            ("0x428","tg-cyrl-tj"), ("0x429","fa-ir"), ("0x42a","vi-vn"), ("0x42b","hy-am"), ("0x42c","az-latn-az"),
            ("0x42d","eu-es"), ("0x42e","hsb-de"), ("0x42f","mk-mk"), ("0x430","st-za"), ("0x431","ts-za"),
            ("0x432","tn-za"), ("0x433","ve-za"), ("0x434","xh-za"), ("0x435","zu-za"), ("0x436","af-za"),
            ("0x437","ka-ge"), ("0x438","fo-fo"), ("0x439","hi-in"), ("0x43a","mt-mt"), ("0x43b","se-no"),
            ("0x43d","yi-001"), ("0x43e","ms-my"), ("0x43f","kk-kz"), ("0x440","ky-kg"), ("0x441","sw-ke"),
            ("0x442","tk-tm"), ("0x443","uz-latn-uz"), ("0x444","tt-ru"), ("0x445","bn-in"), ("0x446","pa-in"),
            ("0x447","gu-in"), ("0x448","or-in"), ("0x449","ta-in"), ("0x44a","te-in"), ("0x44b","kn-in"),
            ("0x44c","ml-in"), ("0x44d","as-in"), ("0x44e","mr-in"), ("0x44f","sa-in"), ("0x450","mn-mn"),
            ("0x451","bo-cn"), ("0x452","cy-gb"), ("0x453","km-kh"), ("0x454","lo-la"), ("0x455","my-mm"),
            ("0x456","gl-es"), ("0x457","kok-in"), ("0x458","mni-in"), ("0x459","sd-deva-in"), ("0x45a","syr-sy"),
            ("0x45b","si-lk"), ("0x45c","chr-cher-us"), ("0x45d","iu-cans-ca"), ("0x45e","am-et"), ("0x45f","tzm-arab-ma"),
            ("0x460","ks-arab"), ("0x461","ne-np"), ("0x462","fy-nl"), ("0x463","ps-af"), ("0x464","fil-ph"),
            ("0x465","dv-mv"), ("0x466","bin-ng"), ("0x467","ff-ng"), ("0x468","ha-latn-ng"), ("0x469","ibb-ng"),
            ("0x46a","yo-ng"), ("0x46b","quz-bo"), ("0x46c","nso-za"), ("0x46d","ba-ru"), ("0x46e","lb-lu"),
            ("0x46f","kl-gl"), ("0x470","ig-ng"), ("0x471","kr-ng"), ("0x472","om-et"), ("0x473","ti-et"),
            ("0x474","gn-py"), ("0x475","haw-us"), ("0x476","la-001"), ("0x477","so-so"), ("0x478","ii-cn"),
            ("0x479","pap-029"), ("0x47a","arn-cl"), ("0x47c","moh-ca"), ("0x47e","br-fr"), ("0x480","ug-cn"),
            ("0x481","mi-nz"), ("0x482","oc-fr"), ("0x483","co-fr"), ("0x484","gsw-fr"), ("0x485","sah-ru"),
            ("0x486","quc-latn-gt"), ("0x487","rw-rw"), ("0x488","wo-sn"), ("0x48c","prs-af"), ("0x491","gd-gb"),
            ("0x492","ku-arab-iq"), ("0x501","qps-ploc"), ("0x5fe","qps-ploca"), ("0x801","ar-iq"), ("0x803","ca-es-valencia"),
            ("0x804","zh-cn"), ("0x807","de-ch"), ("0x809","en-gb"), ("0x80a","es-mx"), ("0x80c","fr-be"),
            ("0x810","it-ch"), ("0x813","nl-be"), ("0x814","nn-no"), ("0x816","pt-pt"), ("0x818","ro-md"),
            ("0x819","ru-md"), ("0x81a","sr-latn-cs"), ("0x81d","sv-fi"), ("0x820","ur-in"), ("0x82c","az-cyrl-az"),
            ("0x82e","dsb-de"), ("0x832","tn-bw"), ("0x83b","se-se"), ("0x83c","ga-ie"), ("0x83e","ms-bn"),
            ("0x843","uz-cyrl-uz"), ("0x845","bn-bd"), ("0x846","pa-arab-pk"), ("0x849","ta-lk"), ("0x850","mn-mong-cn"),
            ("0x859","sd-arab-pk"), ("0x85d","iu-latn-ca"), ("0x85f","tzm-latn-dz"), ("0x860","ks-deva-in"), ("0x861","ne-in"),
            ("0x867","ff-latn-sn"), ("0x86b","quz-ec"), ("0x873","ti-er"), ("0x901","qps-latn-x-sh"), ("0x9ff","qps-plocm"),
            ("0xc01","ar-eg"), ("0xc04","zh-hk"), ("0xc07","de-at"), ("0xc09","en-au"), ("0xc0a","es-es"),
            ("0xc0c","fr-ca"), ("0xc1a","sr-cyrl-cs"), ("0xc3b","se-fi"), ("0xc50","mn-mong-mn"), ("0xc51","dz-bt"),
            ("0xc6b","quz-pe"), ("0x1001","ar-ly"), ("0x1004","zh-sg"), ("0x1007","de-lu"), ("0x1009","en-ca"),
            ("0x100a","es-gt"), ("0x100c","fr-ch"), ("0x101a","hr-ba"), ("0x103b","smj-no"), ("0x105f","tzm-tfng-ma"),
            ("0x1401","ar-dz"), ("0x1404","zh-mo"), ("0x1407","de-li"), ("0x1409","en-nz"), ("0x140a","es-cr"),
            ("0x140c","fr-lu"), ("0x141a","bs-latn-ba"), ("0x143b","smj-se"), ("0x1801","ar-ma"), ("0x1809","en-ie"),
            ("0x180a","es-pa"), ("0x180c","fr-mc"), ("0x181a","sr-latn-ba"), ("0x183b","sma-no"), ("0x1c01","ar-tn"),
            ("0x1c09","en-za"), ("0x1c0a","es-do"), ("0x1c0c","fr-029"), ("0x1c1a","sr-cyrl-ba"), ("0x1c3b","sma-se"),
            ("0x2001","ar-om"), ("0x2009","en-jm"), ("0x200a","es-ve"), ("0x200c","fr-re"), ("0x201a","bs-cyrl-ba"),
            ("0x203b","sms-fi"), ("0x2401","ar-ye"), ("0x2409","en-029"), ("0x240a","es-co"), ("0x240c","fr-cd"),
            ("0x241a","sr-latn-rs"), ("0x243b","smn-fi"), ("0x2801","ar-sy"), ("0x2809","en-bz"), ("0x280a","es-pe"),
            ("0x280c","fr-sn"), ("0x281a","sr-cyrl-rs"), ("0x2c01","ar-jo"), ("0x2c09","en-tt"), ("0x2c0a","es-ar"),
            ("0x2c0c","fr-cm"), ("0x2c1a","sr-latn-me"), ("0x3001","ar-lb"), ("0x3009","en-zw"), ("0x300a","es-ec"),
            ("0x300c","fr-ci"), ("0x301a","sr-cyrl-me"), ("0x3401","ar-kw"), ("0x3409","en-ph"), ("0x340a","es-cl"),
            ("0x340c","fr-ml"), ("0x3801","ar-ae"), ("0x3809","en-id"), ("0x380a","es-uy"), ("0x380c","fr-ma"),
            ("0x3c01","ar-bh"), ("0x3c09","en-hk"), ("0x3c0a","es-py"), ("0x3c0c","fr-ht"), ("0x4001","ar-qa"),
            ("0x4009","en-in"), ("0x400a","es-bo"), ("0x4409","en-my"), ("0x440a","es-sv"), ("0x4809","en-sg"),
            ("0x480a","es-hn"), ("0x4c0a","es-ni"), ("0x500a","es-pr"), ("0x540a","es-us"), ("0x580a","es-419"),
            ("0x5c0a","es-cu"), ("0x641a","bs-cyrl"), ("0x681a","bs-latn"), ("0x6c1a","sr-cyrl"), ("0x701a","sr-latn"),
            ("0x703b","smn"), ("0x742c","az-cyrl"), ("0x743b","sms"), ("0x7804","zh"), ("0x7814","nn"),
            ("0x781a","bs"), ("0x782c","az-latn"), ("0x783b","sma"), ("0x7843","uz-cyrl"), ("0x7850","mn-cyrl"),
            ("0x785d","iu-cans"), ("0x785f","tzm-tfng"), ("0x7c04","zh-cht"), ("0x7c14","nb"), ("0x7c1a","sr"),
            ("0x7c28","tg-cyrl"), ("0x7c2e","dsb"), ("0x7c3b","smj"), ("0x7c43","uz-latn"), ("0x7c46","pa-arab"),
            ("0x7c50","mn-mong"), ("0x7c59","sd-arab"), ("0x7c5c","chr-cher"), ("0x7c5d","iu-latn"), ("0x7c5f","tzm-latn"),
            ("0x7c67","ff-latn"), ("0x7c68","ha-latn"), ("0x7c86","quc-latn"), ("0x7c92","ku-arab"),
            // Sort 0x1
            ("0x1007f","x-iv_mathan"), ("0x10407","de-de_phoneb"), ("0x1040e","hu-hu_technl"), ("0x10437","ka-ge_modern"),
            // Sort 0x2
            ("0x20804","zh-cn_stroke"), ("0x21004","zh-sg_stroke"), ("0x21404","zh-mo_stroke"),
            // Sort 0x3
            ("0x30404","zh-tw_pronun"),
            // Sort 0x4
            ("0x40404","zh-tw_radstr"), ("0x40411","ja-jp_radstr"), ("0x40c04","zh-hk_radstr"), ("0x41404","zh-mo_radstr"),
            // Sort 0x5
            ("0x50804","zh-cn_phoneb"), ("0x51004","zh-sg_phoneb")
        };

        static string[] s_cultures = new string[]
        {
            "aa", "aa-dj", "aa-er", "aa-et",
            "af", "af-na", "af-za",
            "agq", "agq-cm",
            "ak", "ak-gh",
            "am", "am-et",
            "ar", "ar-001", "ar-ae", "ar-bh", "ar-dj", "ar-dz", "ar-eg", "ar-er", "ar-il", "ar-iq", "ar-jo", "ar-km", "ar-kw", "ar-lb", "ar-ly", "ar-ma", "ar-mr", "ar-om", "ar-ps", "ar-qa", "ar-sa", "ar-sd", "ar-so", "ar-ss", "ar-sy", "ar-td", "ar-tn", "ar-ye", "arn", "arn-cl",
            "as", "as-in", "asa", "asa-tz", "ast", "ast-es",
            "az", "az-cyrl", "az-cyrl-az", "az-latn", "az-latn-az",
            "ba", "ba-ru", "bas", "bas-cm",
            "be", "be-by", "bem", "bem-zm", "bez", "bez-tz",
            "bg", "bg-bg",
            "bin", "bin-ng",
            "bm", "bm-latn", "bm-latn-ml",
            "bn", "bn-bd", "bn-in",
            "bo", "bo-cn", "bo-in",
            "br", "br-fr", "brx", "brx-in",
            "bs", "bs-cyrl", "bs-cyrl-ba", "bs-latn", "bs-latn-ba",
            "byn", "byn-er",
            "ca", "ca-ad", "ca-es", "ca-es-valencia", "ca-fr", "ca-it",
            "ce", "ce-ru",
            "cgg", "cgg-ug",
            "chr", "chr-cher", "chr-cher-us",
            "co", "co-fr",
            "cs", "cs-cz",
            "cu", "cu-ru",
            "cy", "cy-gb",
            "da", "da-dk", "da-gl", "dav", "dav-ke",
            "de", "de-at", "de-be", "de-ch", "de-de", "de-de_phoneb", "de-it", "de-li", "de-lu",
            "dje", "dje-ne",
            "dsb", "dsb-de",
            "dua", "dua-cm",
            "dv", "dv-mv",
            "dyo", "dyo-sn",
            "dz", "dz-bt",
            "ebu", "ebu-ke",
            "ee", "ee-gh", "ee-tg",
            "el", "el-cy", "el-gr",
            "en", "en-001", "en-029", "en-150", "en-ag", "en-ai", "en-as", "en-at", "en-au", "en-bb", "en-be", "en-bi", "en-bm", "en-bs", "en-bw", "en-bz", "en-ca", "en-cc", "en-ch", "en-ck", "en-cm", "en-cx", "en-cy", "en-de", "en-dk", "en-dm", "en-er", "en-fi", "en-fj", "en-fk", "en-fm", "en-gb", "en-gd", "en-gg", "en-gh", "en-gi", "en-gm", "en-gu", "en-gy", "en-hk", "en-id", "en-ie", "en-il", "en-im", "en-in", "en-io", "en-je", "en-jm", "en-ke", "en-ki", "en-kn", "en-ky", "en-lc", "en-lr", "en-ls", "en-mg", "en-mh", "en-mo", "en-mp", "en-ms", "en-mt", "en-mu", "en-mw", "en-my", "en-na", "en-nf", "en-ng", "en-nl", "en-nr", "en-nu", "en-nz", "en-pg", "en-ph", "en-pk", "en-pn", "en-pr", "en-pw", "en-rw", "en-sb", "en-sc", "en-sd", "en-se", "en-sg", "en-sh", "en-si", "en-sl", "en-ss", "en-sx", "en-sz", "en-tc", "en-tk", "en-to", "en-tt", "en-tv", "en-tz", "en-ug", "en-um", "en-us", "en-vc", "en-vg", "en-vi", "en-vu", "en-ws", "en-za", "en-zm", "en-zw",
            "eo", "eo-001",
            "es", "es-419", "es-ar", "es-bo", "es-br", "es-cl", "es-co", "es-cr", "es-cu", "es-do", "es-ec", "es-es", "es-es_tradnl", "es-gq", "es-gt", "es-hn", "es-mx", "es-ni", "es-pa", "es-pe", "es-ph", "es-pr", "es-py", "es-sv", "es-us", "es-uy", "es-ve",
            "et", "et-ee",
            "eu", "eu-es",
            "ewo", "ewo-cm",
            "fa", "fa-ir",
            "ff", "ff-cm", "ff-gn", "ff-latn", "ff-latn-sn", "ff-mr", "ff-ng",
            "fi", "fi-fi", "fil", "fil-ph",
            "fo", "fo-dk", "fo-fo",
            "fr", "fr-029", "fr-be", "fr-bf", "fr-bi", "fr-bj", "fr-bl", "fr-ca", "fr-cd", "fr-cf", "fr-cg", "fr-ch", "fr-ci", "fr-cm", "fr-dj", "fr-dz", "fr-fr", "fr-ga", "fr-gf", "fr-gn", "fr-gp", "fr-gq", "fr-ht", "fr-km", "fr-lu", "fr-ma", "fr-mc", "fr-mf", "fr-mg", "fr-ml", "fr-mq", "fr-mr", "fr-mu", "fr-nc", "fr-ne", "fr-pf", "fr-pm", "fr-re", "fr-rw", "fr-sc", "fr-sn", "fr-sy", "fr-td", "fr-tg", "fr-tn", "fr-vu", "fr-wf", "fr-yt",
            "fur", "fur-it",
            "fy", "fy-nl",
            "ga", "ga-ie",
            "gd", "gd-gb",
            "gl", "gl-es",
            "gn", "gn-py",
            "gsw", "gsw-ch", "gsw-fr", "gsw-li",
            "gu", "gu-in", "guz", "guz-ke",
            "gv", "gv-im",
            "ha", "ha-latn", "ha-latn-gh", "ha-latn-ne", "ha-latn-ng", "haw", "haw-us",
            "he", "he-il",
            "hi", "hi-in",
            "hr", "hr-ba", "hr-hr",
            "hsb", "hsb-de",
            "hu", "hu-hu", "hu-hu_technl",
            "hy", "hy-am",
            "ia", "ia-001", "ia-fr",
            "ibb", "ibb-ng",
            "id", "id-id",
            "ig", "ig-ng",
            "ii", "ii-cn",
            "is", "is-is",
            "it", "it-ch", "it-it", "it-sm",
            "iu", "iu-cans", "iu-cans-ca", "iu-latn", "iu-latn-ca",
            "ja", "ja-jp", "ja-jp_radstr",
            "jgo", "jgo-cm",
            "jmc", "jmc-tz",
            "jv", "jv-java", "jv-java-id", "jv-latn", "jv-latn-id",
            "ka", "ka-ge", "ka-ge_modern", "kab", "kab-dz", "kam", "kam-ke",
            "kde", "kde-tz",
            "kea", "kea-cv",
            "khq", "khq-ml",
            "ki", "ki-ke",
            "kk", "kk-kz", "kkj", "kkj-cm",
            "kl", "kl-gl", "kln", "kln-ke",
            "km", "km-kh",
            "kn", "kn-in",
            "ko", "ko-kp", "ko-kr", "kok", "kok-in",
            "kr", "kr-ng",
            "ks", "ks-arab", "ks-arab-in", "ks-deva", "ks-deva-in", "ksb", "ksb-tz", "ksf", "ksf-cm", "ksh", "ksh-de",
            "ku", "ku-arab", "ku-arab-iq", "ku-arab-ir",
            "kw", "kw-gb",
            "ky", "ky-kg",
            "la", "la-001", "lag", "lag-tz",
            "lb", "lb-lu",
            "lg", "lg-ug",
            "lkt", "lkt-us",
            "ln", "ln-ao", "ln-cd", "ln-cf", "ln-cg",
            "lo", "lo-la",
            "lrc", "lrc-iq", "lrc-ir",
            "lt", "lt-lt",
            "lu", "lu-cd", "luo", "luo-ke", "luy", "luy-ke",
            "lv", "lv-lv",
            "mas", "mas-ke", "mas-tz",
            "mer", "mer-ke",
            "mfe", "mfe-mu",
            "mg", "mg-mg", "mgh", "mgh-mz", "mgo", "mgo-cm",
            "mi", "mi-nz",
            "mk", "mk-mk",
            "ml", "ml-in",
            "mn", "mn-cyrl", "mn-mn", "mn-mong", "mn-mong-cn", "mn-mong-mn", "mni", "mni-in",
            "moh", "moh-ca",
            "mr", "mr-in",
            "ms", "ms-bn", "ms-my", "ms-sg",
            "mt", "mt-mt",
            "mua", "mua-cm",
            "my", "my-mm",
            "mzn", "mzn-ir",
            "naq", "naq-na",
            "nb", "nb-no", "nb-sj",
            "nd", "nd-zw", "nds", "nds-de", "nds-nl",
            "ne", "ne-in", "ne-np",
            "nl", "nl-aw", "nl-be", "nl-bq", "nl-cw", "nl-nl", "nl-sr", "nl-sx",
            "nmg", "nmg-cm",
            "nn", "nn-no", "nnh", "nnh-cm",
            "no",
            "nqo", "nqo-gn",
            "nr", "nr-za",
            "nso", "nso-za",
            "nus", "nus-ss",
            "nyn", "nyn-ug",
            "oc", "oc-fr",
            "om", "om-et", "om-ke",
            "or", "or-in",
            "os", "os-ge", "os-ru",
            "pa", "pa-arab", "pa-arab-pk", "pa-in", "pap", "pap-029",
            "pl", "pl-pl",
            "prg", "prg-001", "prs", "prs-af",
            "ps", "ps-af",
            "pt", "pt-ao", "pt-br", "pt-ch", "pt-cv", "pt-gq", "pt-gw", "pt-lu", "pt-mo", "pt-mz", "pt-pt", "pt-st", "pt-tl",
            "qps-latn-x-sh", "qps-ploc", "qps-ploca", "qps-plocm",
            "quc", "quc-latn", "quc-latn-gt", "quz", "quz-bo", "quz-ec", "quz-pe",
            "rm", "rm-ch",
            "rn", "rn-bi",
            "ro", "ro-md", "ro-ro", "rof", "rof-tz",
            "ru", "ru-by", "ru-kg", "ru-kz", "ru-md", "ru-ru", "ru-ua",
            "rw", "rw-rw", "rwk", "rwk-tz",
            "sa", "sa-in", "sah", "sah-ru", "saq", "saq-ke",
            "sbp", "sbp-tz",
            "sd", "sd-arab", "sd-arab-pk", "sd-deva", "sd-deva-in",
            "se", "se-fi", "se-no", "se-se", "seh", "seh-mz", "ses", "ses-ml",
            "sg", "sg-cf",
            "shi", "shi-latn", "shi-latn-ma", "shi-tfng", "shi-tfng-ma",
            "si", "si-lk",
            "sk", "sk-sk",
            "sl", "sl-si",
            "sma", "sma-no", "sma-se", "smj", "smj-no", "smj-se", "smn", "smn-fi", "sms", "sms-fi",
            "sn", "sn-latn", "sn-latn-zw",
            "so", "so-dj", "so-et", "so-ke", "so-so",
            "sq", "sq-al", "sq-mk", "sq-xk",
            "sr", "sr-cyrl", "sr-cyrl-ba", "sr-cyrl-cs", "sr-cyrl-me", "sr-cyrl-rs", "sr-cyrl-xk", "sr-latn", "sr-latn-ba", "sr-latn-cs", "sr-latn-me", "sr-latn-rs", "sr-latn-xk",
            "ss", "ss-sz", "ss-za", "ssy", "ssy-er",
            "st", "st-ls", "st-za",
            "sv", "sv-ax", "sv-fi", "sv-se",
            "sw", "sw-cd", "sw-ke", "sw-tz", "sw-ug", "swc", "swc-cd",
            "syr", "syr-sy",
            "ta", "ta-in", "ta-lk", "ta-my", "ta-sg",
            "te", "te-in", "teo", "teo-ke", "teo-ug",
            "tg", "tg-cyrl", "tg-cyrl-tj",
            "th", "th-th",
            "ti", "ti-er", "ti-et", "tig", "tig-er",
            "tk", "tk-tm",
            "tn", "tn-bw", "tn-za",
            "to", "to-to",
            "tr", "tr-cy", "tr-tr",
            "ts", "ts-za",
            "tt", "tt-ru",
            "twq", "twq-ne",
            "tzm", "tzm-arab", "tzm-arab-ma", "tzm-latn", "tzm-latn-dz", "tzm-latn-ma", "tzm-tfng", "tzm-tfng-ma",
            "ug", "ug-cn",
            "uk", "uk-ua",
            "ur", "ur-in", "ur-pk",
            "uz", "uz-arab", "uz-arab-af", "uz-cyrl", "uz-cyrl-uz", "uz-latn", "uz-latn-uz",
            "vai", "vai-latn", "vai-latn-lr", "vai-vaii", "vai-vaii-lr",
            "ve", "ve-za",
            "vi", "vi-vn",
            "vo", "vo-001",
            "vun", "vun-tz",
            "wae", "wae-ch", "wal", "wal-et",
            "wo", "wo-sn",
            "x-iv_mathan",
            "xh", "xh-za",
            "xog", "xog-ug",
            "yav", "yav-cm",
            "yi", "yi-001",
            "yo", "yo-bj", "yo-ng",
            "yue", "yue-hk",
            "zgh", "zgh-tfng", "zgh-tfng-ma",
            "zh", "zh-chs", "zh-cht", "zh-cn", "zh-cn_phoneb", "zh-cn_stroke", "zh-hans", "zh-hans-hk", "zh-hans-mo", "zh-hant", "zh-hk", "zh-hk_radstr", "zh-mo", "zh-mo_radstr", "zh-mo_stroke", "zh-sg", "zh-sg_phoneb", "zh-sg_stroke", "zh-tw", "zh-tw_pronun", "zh-tw_radstr",
            "zu", "zu-za",
        };

        static void GenerateData(string[] cultures, (string lcid, string culture)[] lcids)
        {
            var list = new List<(string culture, List<string> cultures)>();

            string prev = null;
            for (int i = 0; i < cultures.Length; ++i)
            {
                var raw = cultures[i];

                List<string> values;

                if (i > 0 && raw.StartsWith(prev))
                {
                    values = list[^1].cultures;
                    list[^1] = (raw, values);
                }
                else
                {
                    values = new List<string>();
                    list.Add((raw, values));
                }

                values.Add(raw);
                prev = raw;
                continue;
            }

            Console.WriteLine("private static ReadOnlySpan<byte> CultureNames =>");

            var indexes = new List<(int position, int length, string cultureName)>();
            int pos = 0;
            for (int i = 0; i < list.Count; ++i)
            {
                var row = list[i];

                for (int ii = 0; ii < row.cultures.Count; ++ii)
                {
                    string value = row.cultures[ii];
                    indexes.Add((pos, value.Length, value));
                }

                Console.WriteLine($@"    ""{row.culture}""u8{(i == list.Count - 1 ? ';' : " +")}  // {string.Join(", ", row.cultures)}");
                pos += row.culture.Length;
            }

            Console.WriteLine();
            Console.WriteLine($"private const int CulturesCount = {indexes.Count};");
            Console.WriteLine();

            Console.WriteLine("private static ReadOnlySpan<byte> LocalesNamesIndexes => new byte[CulturesCount * 2]");
            Console.WriteLine("{");

            int max_length = 0;
            foreach (var entry in indexes)
            {
                Debug.Assert(entry.position < Math.Pow(2, 12));
                Debug.Assert(entry.length < Math.Pow(2, 4));

                int index = entry.position << 4 | entry.length;
                int high = index >> 8;
                int low = (byte)index;

                Debug.Assert(((high << 4) | (low >> 4)) == entry.position);
                Debug.Assert((low & 0xF) == entry.length);


                string lookup = $"{high}, {low},";
                Console.WriteLine($"    {lookup}{new string(' ', 10 - lookup.Length)}// {entry.cultureName}");

                max_length = Math.Max(max_length, entry.length);
            }

            Console.WriteLine("};");

            Console.WriteLine();
            Console.WriteLine($"private const int LocaleLongestName = {max_length};");
            Console.WriteLine($"private const int LcidCount = {lcids.Length};");
            Console.WriteLine();

            int lastSort = 0;
            List<(int sort, int index)> sortList = new List<(int sort, int index)>();

            Console.WriteLine("private static ReadOnlySpan<byte> LcidToCultureNameIndices => new byte[LcidCount * 4]");
            Console.WriteLine("{");
            int sortIndex = 0;
            foreach (var entry in lcids)
            {
                int lcid = Convert.ToInt32(entry.lcid, 16);
                int sort = lcid >> 16;
                if (sort != lastSort)
                {
                    Console.WriteLine($"    // Sort 0x{sort:x}");
                    lastSort = sort;
                    sortList.Add((sort, sortIndex * 4));
                }

                string cultureName = entry.culture.ToLowerInvariant();
                int entryIndex = indexes.FindIndex(l => l.cultureName == cultureName);
                if (entryIndex < 0)
                {
                    Console.WriteLine($"{entry.culture} // {cultureName}");
                    continue;
                }

                var str = indexes[entryIndex];
                Debug.Assert(str.position < Math.Pow(2, 12));
                Debug.Assert(str.length < Math.Pow(2, 4));

                // Trim off sort
                lcid = (ushort)lcid;
                int positionLength = str.position << 4 | str.length;

                Console.WriteLine($"    0x{((lcid >> 8) & 0xff):x2}, 0x{(lcid & 0xff):x2}, 0x{((positionLength >> 8) & 0xff):x2}, 0x{(positionLength & 0xff):x2},  // {cultureName}");
                sortIndex++;
            }
            Console.WriteLine("};");

            foreach (var item in sortList)
            {
                Console.WriteLine($"private const int LcidSortPrefix{item.sort}Index = {item.index};");
            }

            Console.WriteLine();
            Console.WriteLine("private const int NumericLocaleDataBytesPerRow = 18;");
            Console.WriteLine();
            Console.WriteLine("private static ReadOnlySpan<byte> LcidToCultureNameIndices => new byte[CulturesCount * NumericLocaleDataBytesPerRow]");
            Console.WriteLine("{");

            for (int i = 0; i < s_nameIndexToNumericData.Length; i += NUMERIC_LOCALE_DATA_COUNT_PER_ROW)
            {
                uint Lcid = (uint)s_nameIndexToNumericData[i];
                uint AnsiCP = (uint)s_nameIndexToNumericData[i + 1];
                uint OemCP = (uint)s_nameIndexToNumericData[i + 2];
                uint MacCP = (uint)s_nameIndexToNumericData[i + 3];
                uint EBCDIC = (uint)s_nameIndexToNumericData[i + 4];
                uint GeoId = (uint)s_nameIndexToNumericData[i + 5];
                uint DigitList = (uint)s_nameIndexToNumericData[i + 6];

                int index = s_nameIndexToNumericData[i + 7];
                Debug.Assert(index == -1 || index < 0xfff);
                uint SpecificCultureIndex = index == -1 ? 0xfff: (uint)index;
                index = s_nameIndexToNumericData[i + 8];
                Debug.Assert(index == -1 || index < 0xfff);
                uint ConsoleLocaleIndex = index == -1 ? 0xfff : (uint)index;

                Debug.Assert(Lcid <=              0xf_ffff);
                Debug.Assert(AnsiCP <=               0xfff);
                Debug.Assert(OemCP <=                0xfff);
                Debug.Assert(MacCP <=               0xffff);
                Debug.Assert(EBCDIC <=              0xffff);
                Debug.Assert(GeoId <=           0xfff_ffff);
                Debug.Assert(DigitList <=             0xff);

                Console.Write("    ");
                Console.Write($"0x{(Lcid >> 16) & 0xf:x2}, 0x{(Lcid >> 8) & 0xff:x2}, 0x{Lcid & 0xff:x2}, ");

                var AnsiOemCP = AnsiCP << 12 | OemCP;
                Console.Write($"0x{(AnsiOemCP >> 16) & 0xff:x2},  0x{(AnsiOemCP >> 8) & 0xff:x2}, 0x{AnsiOemCP & 0xff:x2}, ");

                Console.Write($"0x{(MacCP >> 8) & 0xff:x2}, 0x{MacCP & 0xff:x2},  ");
                Console.Write($"0x{(EBCDIC >> 8) & 0xff:x2}, 0x{EBCDIC & 0xff:x2}, ");

                Console.Write($"0x{(GeoId >> 24) & 0xff:x2}, 0x{(GeoId >> 16) & 0xff:x2},  0x{(GeoId >> 8) & 0xff:x2}, 0x{GeoId & 0xff:x2}, ");
                Console.Write($"0x{DigitList & 0xff:x2}, ");

                var Indices = SpecificCultureIndex << 12 | ConsoleLocaleIndex;

                Console.Write($"0x{(Indices >> 16) & 0xff:x2},  0x{(Indices >> 8) & 0xff:x2}, 0x{Indices & 0xff:x2}, ");
                Console.Write($" // {i / NUMERIC_LOCALE_DATA_COUNT_PER_ROW,-4} - {cultures[i / NUMERIC_LOCALE_DATA_COUNT_PER_ROW]}");
                Console.WriteLine();
            }
            Console.WriteLine("};");
        }
        */
    }
}
