using System;
using System.Collections.Generic;
using System.Text;

namespace R2RDump.x86
{
    class CallPattern
    {

        /// <summary>
        /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/gcdecoder.cpp">src\inc\gcdecoder.cpp</a> decodeCallPattern
        /// </summary>
        public static void DecodeCallPattern(uint pattern, out uint argCnt, out uint regMask, out uint argMask, out uint codeDelta)
        {
            uint val = callPatternTable[pattern];
            byte[] fld = BitConverter.GetBytes(val);
            argCnt = fld[0];
            regMask = fld[1];      // EBP,EBX,ESI,EDI
            argMask = fld[2];
            codeDelta = fld[3];
        }

        /// <summary>
        /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/gcdecoder.cpp">src\inc\gcdecoder.cpp</a> callCommonDelta
        /// </summary>
        public static uint[] callCommonDelta = { 6, 8, 10, 12 };

        /// <summary>
        /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/gcdecoder.cpp">src\inc\gcdecoder.cpp</a> callPatternTable
        /// </summary>
        private static uint[] callPatternTable =
        {
            0x0a000200, //   30109
            0x0c000200, //   22970
            0x0c000201, //   19005
            0x0a000300, //   12193
            0x0c000300, //   10614
            0x0e000200, //   10253
            0x10000200, //    9746
            0x0b000200, //    9698
            0x0d000200, //    9625
            0x08000200, //    8909
            0x0c000301, //    8522
            0x11000200, //    7382
            0x0e000300, //    7357
            0x12000200, //    7139
            0x10000300, //    7062
            0x11000300, //    6970
            0x0a000201, //    6842
            0x0a000100, //    6803
            0x0f000200, //    6795
            0x13000200, //    6559
            0x08000300, //    6079
            0x15000200, //    5874
            0x0d000201, //    5492
            0x0c000100, //    5193
            0x0d000300, //    5165
            0x23000200, //    5143
            0x1b000200, //    5035
            0x14000200, //    4872
            0x0f000300, //    4850
            0x0a000700, //    4781
            0x09000200, //    4560
            0x12000300, //    4496
            0x16000200, //    4180
            0x07000200, //    4021
            0x09000300, //    4012
            0x0c000700, //    3988
            0x0c000600, //    3946
            0x0e000100, //    3823
            0x1a000200, //    3764
            0x18000200, //    3744
            0x17000200, //    3736
            0x1f000200, //    3671
            0x13000300, //    3559
            0x0a000600, //    3214
            0x0e000600, //    3109
            0x08000201, //    2984
            0x0b000300, //    2928
            0x0a000301, //    2859
            0x07000100, //    2826
            0x13000100, //    2782
            0x09000301, //    2644
            0x19000200, //    2638
            0x11000700, //    2618
            0x21000200, //    2518
            0x0d000202, //    2484
            0x10000100, //    2480
            0x0f000600, //    2413
            0x14000300, //    2363
            0x0c000500, //    2362
            0x08000301, //    2285
            0x20000200, //    2245
            0x10000700, //    2240
            0x0f000100, //    2236
            0x1e000200, //    2214
            0x0c000400, //    2193
            0x16000300, //    2171
            0x12000600, //    2132
            0x22000200, //    2011
            0x1d000200, //    2011
            0x0c000f00, //    1996
            0x0e000700, //    1971
            0x0a000400, //    1970
            0x09000201, //    1932
            0x10000600, //    1903
            0x15000300, //    1847
            0x0a000101, //    1814
            0x0a000b00, //    1771
            0x0c000601, //    1737
            0x09000700, //    1737
            0x07000300, //    1684
        };
    }
}
