// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace HFATest
{

    public class TestCase
    {
#pragma warning disable 0414
        const sbyte CONST_INT8 = (sbyte)77;
        const short CONST_INT16 = (short)77;
        const int CONST_INT32 = (int)77;
        const long CONST_INT64 = (long)77;
        const float CONST_FLOAT32 = (float)77.0;
        const double CONST_FLOAT64 = (double)77.0;
#pragma warning restore 0414

        [Fact]
        public static int TestEntryPoint()
        {

            HFA01 hfa01;
            HFA02 hfa02;
            HFA03 hfa03;
            HFA05 hfa05;
            HFA08 hfa08;
            HFA11 hfa11;
            HFA19 hfa19;

            TestMan.Init_HFA01(out hfa01);
            TestMan.Init_HFA02(out hfa02);
            TestMan.Init_HFA03(out hfa03);
            TestMan.Init_HFA05(out hfa05);
            TestMan.Init_HFA08(out hfa08);
            TestMan.Init_HFA11(out hfa11);
            TestMan.Init_HFA19(out hfa19);

            int nFailures = 0;

            nFailures += Common.CheckResult("Average HFA 01", TestMan.Average_HFA01(hfa01), TestMan.EXPECTED_SUM_HFA01 / 1) ? 0 : 1;
            nFailures += Common.CheckResult("Average HFA 02", TestMan.Average_HFA02(hfa02), TestMan.EXPECTED_SUM_HFA02 / 2) ? 0 : 1;
            nFailures += Common.CheckResult("Average HFA 03", TestMan.Average_HFA03(hfa03), TestMan.EXPECTED_SUM_HFA03 / 3) ? 0 : 1;
            nFailures += Common.CheckResult("Average HFA 05", TestMan.Average_HFA05(hfa05), TestMan.EXPECTED_SUM_HFA05 / 5) ? 0 : 1;
            nFailures += Common.CheckResult("Average HFA 08", TestMan.Average_HFA08(hfa08), TestMan.EXPECTED_SUM_HFA08 / 8) ? 0 : 1;
            nFailures += Common.CheckResult("Average HFA 11", TestMan.Average_HFA11(hfa11), TestMan.EXPECTED_SUM_HFA11 / 11) ? 0 : 1;
            nFailures += Common.CheckResult("Average HFA 19", TestMan.Average_HFA19(hfa19), TestMan.EXPECTED_SUM_HFA19 / 19) ? 0 : 1;

            nFailures += Common.CheckResult("Average3 HFA 01", TestMan.Average3_HFA01(hfa01, hfa01, hfa01), TestMan.EXPECTED_SUM_HFA01 / 1) ? 0 : 1;
            nFailures += Common.CheckResult("Average3 HFA 02", TestMan.Average3_HFA02(hfa02, hfa02, hfa02), TestMan.EXPECTED_SUM_HFA02 / 2) ? 0 : 1;
            nFailures += Common.CheckResult("Average3 HFA 03", TestMan.Average3_HFA03(hfa03, hfa03, hfa03), TestMan.EXPECTED_SUM_HFA03 / 3) ? 0 : 1;
            nFailures += Common.CheckResult("Average3 HFA 05", TestMan.Average3_HFA05(hfa05, hfa05, hfa05), TestMan.EXPECTED_SUM_HFA05 / 5) ? 0 : 1;
            nFailures += Common.CheckResult("Average3 HFA 08", TestMan.Average3_HFA08(hfa08, hfa08, hfa08), TestMan.EXPECTED_SUM_HFA08 / 8) ? 0 : 1;
            nFailures += Common.CheckResult("Average3 HFA 11", TestMan.Average3_HFA11(hfa11, hfa11, hfa11), TestMan.EXPECTED_SUM_HFA11 / 11) ? 0 : 1;
            nFailures += Common.CheckResult("Average3 HFA 19", TestMan.Average3_HFA19(hfa19, hfa19, hfa19), TestMan.EXPECTED_SUM_HFA19 / 19) ? 0 : 1;

            nFailures += Common.CheckResult("Average5 HFA 01", TestMan.Average5_HFA01(hfa01, hfa01, hfa01, hfa01, hfa01), TestMan.EXPECTED_SUM_HFA01 / 1) ? 0 : 1;
            nFailures += Common.CheckResult("Average5 HFA 02", TestMan.Average5_HFA02(hfa02, hfa02, hfa02, hfa02, hfa02), TestMan.EXPECTED_SUM_HFA02 / 2) ? 0 : 1;
            nFailures += Common.CheckResult("Average5 HFA 03", TestMan.Average5_HFA03(hfa03, hfa03, hfa03, hfa03, hfa03), TestMan.EXPECTED_SUM_HFA03 / 3) ? 0 : 1;
            nFailures += Common.CheckResult("Average5 HFA 05", TestMan.Average5_HFA05(hfa05, hfa05, hfa05, hfa05, hfa05), TestMan.EXPECTED_SUM_HFA05 / 5) ? 0 : 1;
            nFailures += Common.CheckResult("Average5 HFA 08", TestMan.Average5_HFA08(hfa08, hfa08, hfa08, hfa08, hfa08), TestMan.EXPECTED_SUM_HFA08 / 8) ? 0 : 1;
            nFailures += Common.CheckResult("Average5 HFA 11", TestMan.Average5_HFA11(hfa11, hfa11, hfa11, hfa11, hfa11), TestMan.EXPECTED_SUM_HFA11 / 11) ? 0 : 1;
            nFailures += Common.CheckResult("Average5 HFA 19", TestMan.Average5_HFA19(hfa19, hfa19, hfa19, hfa19, hfa19), TestMan.EXPECTED_SUM_HFA19 / 19) ? 0 : 1;

            nFailures += Common.CheckResult("Average8 HFA 01", TestMan.Average8_HFA01(hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01), TestMan.EXPECTED_SUM_HFA01 / 1) ? 0 : 1;
            nFailures += Common.CheckResult("Average8 HFA 02", TestMan.Average8_HFA02(hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02), TestMan.EXPECTED_SUM_HFA02 / 2) ? 0 : 1;
            nFailures += Common.CheckResult("Average8 HFA 03", TestMan.Average8_HFA03(hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03), TestMan.EXPECTED_SUM_HFA03 / 3) ? 0 : 1;
            nFailures += Common.CheckResult("Average8 HFA 05", TestMan.Average8_HFA05(hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05), TestMan.EXPECTED_SUM_HFA05 / 5) ? 0 : 1;
            nFailures += Common.CheckResult("Average8 HFA 08", TestMan.Average8_HFA08(hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08), TestMan.EXPECTED_SUM_HFA08 / 8) ? 0 : 1;
            nFailures += Common.CheckResult("Average8 HFA 11", TestMan.Average8_HFA11(hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11), TestMan.EXPECTED_SUM_HFA11 / 11) ? 0 : 1;
            nFailures += Common.CheckResult("Average8 HFA 19", TestMan.Average8_HFA19(hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19), TestMan.EXPECTED_SUM_HFA19 / 19) ? 0 : 1;

            nFailures += Common.CheckResult("Average11 HFA 01", TestMan.Average11_HFA01(hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01), TestMan.EXPECTED_SUM_HFA01 / 1) ? 0 : 1;
            nFailures += Common.CheckResult("Average11 HFA 02", TestMan.Average11_HFA02(hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02), TestMan.EXPECTED_SUM_HFA02 / 2) ? 0 : 1;
            nFailures += Common.CheckResult("Average11 HFA 03", TestMan.Average11_HFA03(hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03), TestMan.EXPECTED_SUM_HFA03 / 3) ? 0 : 1;
            nFailures += Common.CheckResult("Average11 HFA 05", TestMan.Average11_HFA05(hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05), TestMan.EXPECTED_SUM_HFA05 / 5) ? 0 : 1;
            nFailures += Common.CheckResult("Average11 HFA 08", TestMan.Average11_HFA08(hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08), TestMan.EXPECTED_SUM_HFA08 / 8) ? 0 : 1;
            nFailures += Common.CheckResult("Average11 HFA 11", TestMan.Average11_HFA11(hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11), TestMan.EXPECTED_SUM_HFA11 / 11) ? 0 : 1;
            nFailures += Common.CheckResult("Average11 HFA 19", TestMan.Average11_HFA19(hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19), TestMan.EXPECTED_SUM_HFA19 / 19) ? 0 : 1;

            nFailures += Common.CheckResult("Average19 HFA 01", TestMan.Average19_HFA01(hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01, hfa01), TestMan.EXPECTED_SUM_HFA01 / 1) ? 0 : 1;
            nFailures += Common.CheckResult("Average19 HFA 02", TestMan.Average19_HFA02(hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02, hfa02), TestMan.EXPECTED_SUM_HFA02 / 2) ? 0 : 1;
            nFailures += Common.CheckResult("Average19 HFA 03", TestMan.Average19_HFA03(hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03, hfa03), TestMan.EXPECTED_SUM_HFA03 / 3) ? 0 : 1;
            nFailures += Common.CheckResult("Average19 HFA 05", TestMan.Average19_HFA05(hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05, hfa05), TestMan.EXPECTED_SUM_HFA05 / 5) ? 0 : 1;
            nFailures += Common.CheckResult("Average19 HFA 08", TestMan.Average19_HFA08(hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08, hfa08), TestMan.EXPECTED_SUM_HFA08 / 8) ? 0 : 1;
            nFailures += Common.CheckResult("Average19 HFA 11", TestMan.Average19_HFA11(hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11, hfa11), TestMan.EXPECTED_SUM_HFA11 / 11) ? 0 : 1;
            nFailures += Common.CheckResult("Average19 HFA 19", TestMan.Average19_HFA19(hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19, hfa19), TestMan.EXPECTED_SUM_HFA19 / 19) ? 0 : 1;

            return nFailures == 0 ? Common.SUCC_RET_CODE : Common.FAIL_RET_CODE;
        }
    }
}
