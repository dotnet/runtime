// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace HFATest
{

    public class TestCase
    {
        const sbyte CONST_INT8 = (sbyte)-128;
#pragma warning disable 0414
        const short CONST_INT16 = (short)-128;
#pragma warning restore 0414
        const int CONST_INT32 = (int)-128;
        const long CONST_INT64 = (long)-128;
        const float CONST_FLOAT32 = (float)-128.0;
        const double CONST_FLOAT64 = (double)-128.0;

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

            nFailures += Common.CheckResult("Sum HFA 01", TestMan.Sum_HFA01(hfa01), TestMan.EXPECTED_SUM_HFA01) ? 0 : 1;
            nFailures += Common.CheckResult("Sum HFA 02", TestMan.Sum_HFA02(hfa02), TestMan.EXPECTED_SUM_HFA02) ? 0 : 1;
            nFailures += Common.CheckResult("Sum HFA 03", TestMan.Sum_HFA03(hfa03), TestMan.EXPECTED_SUM_HFA03) ? 0 : 1;
            nFailures += Common.CheckResult("Sum HFA 05", TestMan.Sum_HFA05(hfa05), TestMan.EXPECTED_SUM_HFA05) ? 0 : 1;
            nFailures += Common.CheckResult("Sum HFA 08", TestMan.Sum_HFA08(hfa08), TestMan.EXPECTED_SUM_HFA08) ? 0 : 1;
            nFailures += Common.CheckResult("Sum HFA 11", TestMan.Sum_HFA11(hfa11), TestMan.EXPECTED_SUM_HFA11) ? 0 : 1;
            nFailures += Common.CheckResult("Sum HFA 19", TestMan.Sum_HFA19(hfa19), TestMan.EXPECTED_SUM_HFA19) ? 0 : 1;

            nFailures += Common.CheckResult("Sum3 HFA 01", TestMan.Sum3_HFA01(CONST_FLOAT32, -CONST_INT64, hfa01), TestMan.EXPECTED_SUM_HFA01) ? 0 : 1;
            nFailures += Common.CheckResult("Sum3 HFA 02", TestMan.Sum3_HFA02(CONST_FLOAT32, -CONST_INT64, hfa02), TestMan.EXPECTED_SUM_HFA02) ? 0 : 1;
            nFailures += Common.CheckResult("Sum3 HFA 03", TestMan.Sum3_HFA03(CONST_FLOAT32, -CONST_INT64, hfa03), TestMan.EXPECTED_SUM_HFA03) ? 0 : 1;
            nFailures += Common.CheckResult("Sum3 HFA 05", TestMan.Sum3_HFA05(CONST_FLOAT32, -CONST_INT64, hfa05), TestMan.EXPECTED_SUM_HFA05) ? 0 : 1;
            nFailures += Common.CheckResult("Sum3 HFA 08", TestMan.Sum3_HFA08(CONST_FLOAT32, -CONST_INT64, hfa08), TestMan.EXPECTED_SUM_HFA08) ? 0 : 1;
            nFailures += Common.CheckResult("Sum3 HFA 11", TestMan.Sum3_HFA11(CONST_FLOAT32, -CONST_INT64, hfa11), TestMan.EXPECTED_SUM_HFA11) ? 0 : 1;
            nFailures += Common.CheckResult("Sum3 HFA 19", TestMan.Sum3_HFA19(CONST_FLOAT32, -CONST_INT64, hfa19), TestMan.EXPECTED_SUM_HFA19) ? 0 : 1;

            nFailures += Common.CheckResult("Sum5 HFA 01", TestMan.Sum5_HFA01(CONST_INT64, -CONST_FLOAT64, -CONST_INT32, CONST_INT8, hfa01), TestMan.EXPECTED_SUM_HFA01) ? 0 : 1;
            nFailures += Common.CheckResult("Sum5 HFA 02", TestMan.Sum5_HFA02(CONST_INT64, -CONST_FLOAT64, -CONST_INT32, CONST_INT8, hfa02), TestMan.EXPECTED_SUM_HFA02) ? 0 : 1;
            nFailures += Common.CheckResult("Sum5 HFA 03", TestMan.Sum5_HFA03(CONST_INT64, -CONST_FLOAT64, -CONST_INT32, CONST_INT8, hfa03), TestMan.EXPECTED_SUM_HFA03) ? 0 : 1;
            nFailures += Common.CheckResult("Sum5 HFA 05", TestMan.Sum5_HFA05(CONST_INT64, -CONST_FLOAT64, -CONST_INT32, CONST_INT8, hfa05), TestMan.EXPECTED_SUM_HFA05) ? 0 : 1;
            nFailures += Common.CheckResult("Sum5 HFA 08", TestMan.Sum5_HFA08(CONST_INT64, -CONST_FLOAT64, -CONST_INT32, CONST_INT8, hfa08), TestMan.EXPECTED_SUM_HFA08) ? 0 : 1;
            nFailures += Common.CheckResult("Sum5 HFA 11", TestMan.Sum5_HFA11(CONST_INT64, -CONST_FLOAT64, -CONST_INT32, CONST_INT8, hfa11), TestMan.EXPECTED_SUM_HFA11) ? 0 : 1;
            nFailures += Common.CheckResult("Sum5 HFA 19", TestMan.Sum5_HFA19(CONST_INT64, -CONST_FLOAT64, -CONST_INT32, CONST_INT8, hfa19), TestMan.EXPECTED_SUM_HFA19) ? 0 : 1;

            nFailures += Common.CheckResult("Sum8 HFA 01", TestMan.Sum8_HFA01(CONST_FLOAT32, -CONST_FLOAT64, -CONST_INT64, CONST_INT8, CONST_FLOAT64, hfa01), TestMan.EXPECTED_SUM_HFA01 + CONST_INT8) ? 0 : 1;
            nFailures += Common.CheckResult("Sum8 HFA 02", TestMan.Sum8_HFA02(CONST_FLOAT32, -CONST_FLOAT64, -CONST_INT64, CONST_INT8, CONST_FLOAT64, hfa02), TestMan.EXPECTED_SUM_HFA02 + CONST_INT8) ? 0 : 1;
            nFailures += Common.CheckResult("Sum8 HFA 03", TestMan.Sum8_HFA03(CONST_FLOAT32, -CONST_FLOAT64, -CONST_INT64, CONST_INT8, CONST_FLOAT64, hfa03), TestMan.EXPECTED_SUM_HFA03 + CONST_INT8) ? 0 : 1;
            nFailures += Common.CheckResult("Sum8 HFA 05", TestMan.Sum8_HFA05(CONST_FLOAT32, -CONST_FLOAT64, -CONST_INT64, CONST_INT8, CONST_FLOAT64, hfa05), TestMan.EXPECTED_SUM_HFA05 + CONST_INT8) ? 0 : 1;
            nFailures += Common.CheckResult("Sum8 HFA 08", TestMan.Sum8_HFA08(CONST_FLOAT32, -CONST_FLOAT64, -CONST_INT64, CONST_INT8, CONST_FLOAT64, hfa08), TestMan.EXPECTED_SUM_HFA08 + CONST_INT8) ? 0 : 1;
            nFailures += Common.CheckResult("Sum8 HFA 11", TestMan.Sum8_HFA11(CONST_FLOAT32, -CONST_FLOAT64, -CONST_INT64, CONST_INT8, CONST_FLOAT64, hfa11), TestMan.EXPECTED_SUM_HFA11 + CONST_INT8) ? 0 : 1;
            nFailures += Common.CheckResult("Sum8 HFA 19", TestMan.Sum8_HFA19(CONST_FLOAT32, -CONST_FLOAT64, -CONST_INT64, CONST_INT8, CONST_FLOAT64, hfa19), TestMan.EXPECTED_SUM_HFA19 + CONST_INT8) ? 0 : 1;

            nFailures += Common.CheckResult("Sum11 HFA 01", TestMan.Sum11_HFA01(-CONST_FLOAT64, -CONST_FLOAT32, -CONST_FLOAT32, -CONST_INT32, CONST_FLOAT32, CONST_INT64, CONST_FLOAT64, CONST_FLOAT32, hfa01), TestMan.EXPECTED_SUM_HFA01) ? 0 : 1;
            nFailures += Common.CheckResult("Sum11 HFA 02", TestMan.Sum11_HFA02(-CONST_FLOAT64, -CONST_FLOAT32, -CONST_FLOAT32, -CONST_INT32, CONST_FLOAT32, CONST_INT64, CONST_FLOAT64, CONST_FLOAT32, hfa02), TestMan.EXPECTED_SUM_HFA02) ? 0 : 1;
            nFailures += Common.CheckResult("Sum11 HFA 03", TestMan.Sum11_HFA03(-CONST_FLOAT64, -CONST_FLOAT32, -CONST_FLOAT32, -CONST_INT32, CONST_FLOAT32, CONST_INT64, CONST_FLOAT64, CONST_FLOAT32, hfa03), TestMan.EXPECTED_SUM_HFA03) ? 0 : 1;
            nFailures += Common.CheckResult("Sum11 HFA 05", TestMan.Sum11_HFA05(-CONST_FLOAT64, -CONST_FLOAT32, -CONST_FLOAT32, -CONST_INT32, CONST_FLOAT32, CONST_INT64, CONST_FLOAT64, CONST_FLOAT32, hfa05), TestMan.EXPECTED_SUM_HFA05) ? 0 : 1;
            nFailures += Common.CheckResult("Sum11 HFA 08", TestMan.Sum11_HFA08(-CONST_FLOAT64, -CONST_FLOAT32, -CONST_FLOAT32, -CONST_INT32, CONST_FLOAT32, CONST_INT64, CONST_FLOAT64, CONST_FLOAT32, hfa08), TestMan.EXPECTED_SUM_HFA08) ? 0 : 1;
            nFailures += Common.CheckResult("Sum11 HFA 11", TestMan.Sum11_HFA11(-CONST_FLOAT64, -CONST_FLOAT32, -CONST_FLOAT32, -CONST_INT32, CONST_FLOAT32, CONST_INT64, CONST_FLOAT64, CONST_FLOAT32, hfa11), TestMan.EXPECTED_SUM_HFA11) ? 0 : 1;
            nFailures += Common.CheckResult("Sum11 HFA 19", TestMan.Sum11_HFA19(-CONST_FLOAT64, -CONST_FLOAT32, -CONST_FLOAT32, -CONST_INT32, CONST_FLOAT32, CONST_INT64, CONST_FLOAT64, CONST_FLOAT32, hfa19), TestMan.EXPECTED_SUM_HFA19) ? 0 : 1;

            nFailures += Common.CheckResult("Sum19 HFA 01", TestMan.Sum19_HFA01(CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, -CONST_FLOAT32, hfa01), TestMan.EXPECTED_SUM_HFA01 - CONST_FLOAT32) ? 0 : 1;
            nFailures += Common.CheckResult("Sum19 HFA 02", TestMan.Sum19_HFA02(CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, -CONST_FLOAT32, hfa02), TestMan.EXPECTED_SUM_HFA02 - CONST_FLOAT32) ? 0 : 1;
            nFailures += Common.CheckResult("Sum19 HFA 03", TestMan.Sum19_HFA03(CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, -CONST_FLOAT32, hfa03), TestMan.EXPECTED_SUM_HFA03 - CONST_FLOAT32) ? 0 : 1;
            nFailures += Common.CheckResult("Sum19 HFA 05", TestMan.Sum19_HFA05(CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, -CONST_FLOAT32, hfa05), TestMan.EXPECTED_SUM_HFA05 - CONST_FLOAT32) ? 0 : 1;
            nFailures += Common.CheckResult("Sum19 HFA 08", TestMan.Sum19_HFA08(CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, -CONST_FLOAT32, hfa08), TestMan.EXPECTED_SUM_HFA08 - CONST_FLOAT32) ? 0 : 1;
            nFailures += Common.CheckResult("Sum19 HFA 11", TestMan.Sum19_HFA11(CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, -CONST_FLOAT32, hfa11), TestMan.EXPECTED_SUM_HFA11 - CONST_FLOAT32) ? 0 : 1;
            nFailures += Common.CheckResult("Sum19 HFA 19", TestMan.Sum19_HFA19(CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, CONST_FLOAT32, -CONST_FLOAT64, -CONST_FLOAT32, hfa19), TestMan.EXPECTED_SUM_HFA19 - CONST_FLOAT32) ? 0 : 1;

            return nFailures == 0 ? Common.SUCC_RET_CODE : Common.FAIL_RET_CODE;
        }
    }
}
