// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace HFATest
{

    public class TestCase
    {
        const sbyte CONST_INT8 = (sbyte)-18;
        const short CONST_INT16 = (short)369;
        const int CONST_INT32 = (int)-987744;
        const long CONST_INT64 = (long)2455782;
        const float CONST_FLOAT32 = (float)12874.00;
        const double CONST_FLOAT64 = (double)-57168.00;

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

            float ADD01_EXP = (float)CONST_INT16 + (float)CONST_INT32 + (float)CONST_INT64 + (4 * CONST_FLOAT32) + (float)CONST_FLOAT64;
            nFailures += Common.CheckResult("Add01 HFA 01", TestMan.Add01_HFA01(hfa01, CONST_FLOAT32, hfa01, CONST_INT32, hfa01, CONST_INT16, CONST_FLOAT64, hfa01, hfa01, CONST_FLOAT32, CONST_INT64, CONST_FLOAT32, hfa01, CONST_FLOAT32, hfa01), 7 * TestMan.EXPECTED_SUM_HFA01 + ADD01_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add01 HFA 02", TestMan.Add01_HFA02(hfa02, CONST_FLOAT32, hfa02, CONST_INT32, hfa02, CONST_INT16, CONST_FLOAT64, hfa02, hfa02, CONST_FLOAT32, CONST_INT64, CONST_FLOAT32, hfa02, CONST_FLOAT32, hfa02), 7 * TestMan.EXPECTED_SUM_HFA02 + ADD01_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add01 HFA 03", TestMan.Add01_HFA03(hfa03, CONST_FLOAT32, hfa03, CONST_INT32, hfa03, CONST_INT16, CONST_FLOAT64, hfa03, hfa03, CONST_FLOAT32, CONST_INT64, CONST_FLOAT32, hfa03, CONST_FLOAT32, hfa03), 7 * TestMan.EXPECTED_SUM_HFA03 + ADD01_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add01 HFA 05", TestMan.Add01_HFA05(hfa05, CONST_FLOAT32, hfa05, CONST_INT32, hfa05, CONST_INT16, CONST_FLOAT64, hfa05, hfa05, CONST_FLOAT32, CONST_INT64, CONST_FLOAT32, hfa05, CONST_FLOAT32, hfa05), 7 * TestMan.EXPECTED_SUM_HFA05 + ADD01_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add01 HFA 08", TestMan.Add01_HFA08(hfa08, CONST_FLOAT32, hfa08, CONST_INT32, hfa08, CONST_INT16, CONST_FLOAT64, hfa08, hfa08, CONST_FLOAT32, CONST_INT64, CONST_FLOAT32, hfa08, CONST_FLOAT32, hfa08), 7 * TestMan.EXPECTED_SUM_HFA08 + ADD01_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add01 HFA 11", TestMan.Add01_HFA11(hfa11, CONST_FLOAT32, hfa11, CONST_INT32, hfa11, CONST_INT16, CONST_FLOAT64, hfa11, hfa11, CONST_FLOAT32, CONST_INT64, CONST_FLOAT32, hfa11, CONST_FLOAT32, hfa11), 7 * TestMan.EXPECTED_SUM_HFA11 + ADD01_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add01 HFA 19", TestMan.Add01_HFA19(hfa19, CONST_FLOAT32, hfa19, CONST_INT32, hfa19, CONST_INT16, CONST_FLOAT64, hfa19, hfa19, CONST_FLOAT32, CONST_INT64, CONST_FLOAT32, hfa19, CONST_FLOAT32, hfa19), 7 * TestMan.EXPECTED_SUM_HFA19 + ADD01_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add01 HFA 00", TestMan.Add01_HFA00(hfa03, CONST_FLOAT32, hfa02, CONST_INT32, hfa19, CONST_INT16, CONST_FLOAT64, hfa05, hfa08, CONST_FLOAT32, CONST_INT64, CONST_FLOAT32, hfa11, CONST_FLOAT32, hfa01), TestMan.EXPECTED_SUM_HFA01 + TestMan.EXPECTED_SUM_HFA02 + TestMan.EXPECTED_SUM_HFA03 + TestMan.EXPECTED_SUM_HFA05 + TestMan.EXPECTED_SUM_HFA08 + TestMan.EXPECTED_SUM_HFA11 + TestMan.EXPECTED_SUM_HFA19 + ADD01_EXP) ? 0 : 1;

            float ADD02_EXP = (2 * (float)CONST_INT16) + (float)CONST_INT32 + (float)CONST_INT64 + (4 * CONST_FLOAT32) + (2 * (float)CONST_FLOAT64);
            nFailures += Common.CheckResult("Add02 HFA 01", TestMan.Add02_HFA01(hfa01, hfa01, CONST_INT64, CONST_INT16, CONST_FLOAT32, CONST_INT32, CONST_FLOAT64, CONST_FLOAT32, hfa01, CONST_FLOAT64, CONST_FLOAT32, hfa01, CONST_INT16, hfa01, CONST_FLOAT32, hfa01, hfa01), 7 * TestMan.EXPECTED_SUM_HFA01 + ADD02_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add02 HFA 02", TestMan.Add02_HFA02(hfa02, hfa02, CONST_INT64, CONST_INT16, CONST_FLOAT32, CONST_INT32, CONST_FLOAT64, CONST_FLOAT32, hfa02, CONST_FLOAT64, CONST_FLOAT32, hfa02, CONST_INT16, hfa02, CONST_FLOAT32, hfa02, hfa02), 7 * TestMan.EXPECTED_SUM_HFA02 + ADD02_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add02 HFA 03", TestMan.Add02_HFA03(hfa03, hfa03, CONST_INT64, CONST_INT16, CONST_FLOAT32, CONST_INT32, CONST_FLOAT64, CONST_FLOAT32, hfa03, CONST_FLOAT64, CONST_FLOAT32, hfa03, CONST_INT16, hfa03, CONST_FLOAT32, hfa03, hfa03), 7 * TestMan.EXPECTED_SUM_HFA03 + ADD02_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add02 HFA 05", TestMan.Add02_HFA05(hfa05, hfa05, CONST_INT64, CONST_INT16, CONST_FLOAT32, CONST_INT32, CONST_FLOAT64, CONST_FLOAT32, hfa05, CONST_FLOAT64, CONST_FLOAT32, hfa05, CONST_INT16, hfa05, CONST_FLOAT32, hfa05, hfa05), 7 * TestMan.EXPECTED_SUM_HFA05 + ADD02_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add02 HFA 08", TestMan.Add02_HFA08(hfa08, hfa08, CONST_INT64, CONST_INT16, CONST_FLOAT32, CONST_INT32, CONST_FLOAT64, CONST_FLOAT32, hfa08, CONST_FLOAT64, CONST_FLOAT32, hfa08, CONST_INT16, hfa08, CONST_FLOAT32, hfa08, hfa08), 7 * TestMan.EXPECTED_SUM_HFA08 + ADD02_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add02 HFA 11", TestMan.Add02_HFA11(hfa11, hfa11, CONST_INT64, CONST_INT16, CONST_FLOAT32, CONST_INT32, CONST_FLOAT64, CONST_FLOAT32, hfa11, CONST_FLOAT64, CONST_FLOAT32, hfa11, CONST_INT16, hfa11, CONST_FLOAT32, hfa11, hfa11), 7 * TestMan.EXPECTED_SUM_HFA11 + ADD02_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add02 HFA 19", TestMan.Add02_HFA19(hfa19, hfa19, CONST_INT64, CONST_INT16, CONST_FLOAT32, CONST_INT32, CONST_FLOAT64, CONST_FLOAT32, hfa19, CONST_FLOAT64, CONST_FLOAT32, hfa19, CONST_INT16, hfa19, CONST_FLOAT32, hfa19, hfa19), 7 * TestMan.EXPECTED_SUM_HFA19 + ADD02_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add02 HFA 00", TestMan.Add02_HFA00(hfa01, hfa05, CONST_INT64, CONST_INT16, CONST_FLOAT32, CONST_INT32, CONST_FLOAT64, CONST_FLOAT32, hfa03, CONST_FLOAT64, CONST_FLOAT32, hfa11, CONST_INT16, hfa19, CONST_FLOAT32, hfa08, hfa02), TestMan.EXPECTED_SUM_HFA01 + TestMan.EXPECTED_SUM_HFA02 + TestMan.EXPECTED_SUM_HFA03 + TestMan.EXPECTED_SUM_HFA05 + TestMan.EXPECTED_SUM_HFA08 + TestMan.EXPECTED_SUM_HFA11 + TestMan.EXPECTED_SUM_HFA19 + ADD02_EXP) ? 0 : 1;

            float ADD03_EXP = (2 * (float)CONST_INT8) + (float)CONST_INT16 + (float)CONST_INT32 + (float)CONST_INT64 + (4 * CONST_FLOAT32) + (float)CONST_FLOAT64;
            nFailures += Common.CheckResult("Add03 HFA 01", TestMan.Add03_HFA01(CONST_FLOAT32, CONST_INT8, hfa01, CONST_FLOAT64, CONST_INT8, hfa01, CONST_INT64, CONST_INT16, CONST_INT32, hfa01, hfa01, CONST_FLOAT32, hfa01, CONST_FLOAT32, hfa01, CONST_FLOAT32, hfa01), 7 * TestMan.EXPECTED_SUM_HFA01 + ADD03_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add03 HFA 02", TestMan.Add03_HFA02(CONST_FLOAT32, CONST_INT8, hfa02, CONST_FLOAT64, CONST_INT8, hfa02, CONST_INT64, CONST_INT16, CONST_INT32, hfa02, hfa02, CONST_FLOAT32, hfa02, CONST_FLOAT32, hfa02, CONST_FLOAT32, hfa02), 7 * TestMan.EXPECTED_SUM_HFA02 + ADD03_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add03 HFA 03", TestMan.Add03_HFA03(CONST_FLOAT32, CONST_INT8, hfa03, CONST_FLOAT64, CONST_INT8, hfa03, CONST_INT64, CONST_INT16, CONST_INT32, hfa03, hfa03, CONST_FLOAT32, hfa03, CONST_FLOAT32, hfa03, CONST_FLOAT32, hfa03), 7 * TestMan.EXPECTED_SUM_HFA03 + ADD03_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add03 HFA 05", TestMan.Add03_HFA05(CONST_FLOAT32, CONST_INT8, hfa05, CONST_FLOAT64, CONST_INT8, hfa05, CONST_INT64, CONST_INT16, CONST_INT32, hfa05, hfa05, CONST_FLOAT32, hfa05, CONST_FLOAT32, hfa05, CONST_FLOAT32, hfa05), 7 * TestMan.EXPECTED_SUM_HFA05 + ADD03_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add03 HFA 08", TestMan.Add03_HFA08(CONST_FLOAT32, CONST_INT8, hfa08, CONST_FLOAT64, CONST_INT8, hfa08, CONST_INT64, CONST_INT16, CONST_INT32, hfa08, hfa08, CONST_FLOAT32, hfa08, CONST_FLOAT32, hfa08, CONST_FLOAT32, hfa08), 7 * TestMan.EXPECTED_SUM_HFA08 + ADD03_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add03 HFA 11", TestMan.Add03_HFA11(CONST_FLOAT32, CONST_INT8, hfa11, CONST_FLOAT64, CONST_INT8, hfa11, CONST_INT64, CONST_INT16, CONST_INT32, hfa11, hfa11, CONST_FLOAT32, hfa11, CONST_FLOAT32, hfa11, CONST_FLOAT32, hfa11), 7 * TestMan.EXPECTED_SUM_HFA11 + ADD03_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add03 HFA 19", TestMan.Add03_HFA19(CONST_FLOAT32, CONST_INT8, hfa19, CONST_FLOAT64, CONST_INT8, hfa19, CONST_INT64, CONST_INT16, CONST_INT32, hfa19, hfa19, CONST_FLOAT32, hfa19, CONST_FLOAT32, hfa19, CONST_FLOAT32, hfa19), 7 * TestMan.EXPECTED_SUM_HFA19 + ADD03_EXP) ? 0 : 1;
            nFailures += Common.CheckResult("Add03 HFA 00", TestMan.Add03_HFA00(CONST_FLOAT32, CONST_INT8, hfa08, CONST_FLOAT64, CONST_INT8, hfa19, CONST_INT64, CONST_INT16, CONST_INT32, hfa03, hfa01, CONST_FLOAT32, hfa11, CONST_FLOAT32, hfa02, CONST_FLOAT32, hfa05), TestMan.EXPECTED_SUM_HFA01 + TestMan.EXPECTED_SUM_HFA02 + TestMan.EXPECTED_SUM_HFA03 + TestMan.EXPECTED_SUM_HFA05 + TestMan.EXPECTED_SUM_HFA08 + TestMan.EXPECTED_SUM_HFA11 + TestMan.EXPECTED_SUM_HFA19 + ADD03_EXP) ? 0 : 1;

            return nFailures == 0 ? Common.SUCC_RET_CODE : Common.FAIL_RET_CODE;
        }
    }
}
