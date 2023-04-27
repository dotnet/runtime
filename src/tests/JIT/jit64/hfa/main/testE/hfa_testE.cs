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

            nFailures += Common.CheckResult("Identity HFA 01", TestMan.Sum_HFA01(TestMan.Identity_HFA01(hfa01)), TestMan.EXPECTED_SUM_HFA01) ? 0 : 1;
            nFailures += Common.CheckResult("Identity HFA 02", TestMan.Sum_HFA02(TestMan.Identity_HFA02(hfa02)), TestMan.EXPECTED_SUM_HFA02) ? 0 : 1;
            nFailures += Common.CheckResult("Identity HFA 03", TestMan.Sum_HFA03(TestMan.Identity_HFA03(hfa03)), TestMan.EXPECTED_SUM_HFA03) ? 0 : 1;
            nFailures += Common.CheckResult("Identity HFA 05", TestMan.Sum_HFA05(TestMan.Identity_HFA05(hfa05)), TestMan.EXPECTED_SUM_HFA05) ? 0 : 1;
            nFailures += Common.CheckResult("Identity HFA 08", TestMan.Sum_HFA08(TestMan.Identity_HFA08(hfa08)), TestMan.EXPECTED_SUM_HFA08) ? 0 : 1;
            nFailures += Common.CheckResult("Identity HFA 11", TestMan.Sum_HFA11(TestMan.Identity_HFA11(hfa11)), TestMan.EXPECTED_SUM_HFA11) ? 0 : 1;
            nFailures += Common.CheckResult("Identity HFA 19", TestMan.Sum_HFA19(TestMan.Identity_HFA19(hfa19)), TestMan.EXPECTED_SUM_HFA19) ? 0 : 1;

            nFailures += Common.CheckResult("Get HFA 01", TestMan.Sum_HFA01(TestMan.Get_HFA01()), TestMan.EXPECTED_SUM_HFA01) ? 0 : 1;
            nFailures += Common.CheckResult("Get HFA 02", TestMan.Sum_HFA02(TestMan.Get_HFA02()), TestMan.EXPECTED_SUM_HFA02) ? 0 : 1;
            nFailures += Common.CheckResult("Get HFA 03", TestMan.Sum_HFA03(TestMan.Get_HFA03()), TestMan.EXPECTED_SUM_HFA03) ? 0 : 1;
            nFailures += Common.CheckResult("Get HFA 05", TestMan.Sum_HFA05(TestMan.Get_HFA05()), TestMan.EXPECTED_SUM_HFA05) ? 0 : 1;
            nFailures += Common.CheckResult("Get HFA 08", TestMan.Sum_HFA08(TestMan.Get_HFA08()), TestMan.EXPECTED_SUM_HFA08) ? 0 : 1;
            nFailures += Common.CheckResult("Get HFA 11", TestMan.Sum_HFA11(TestMan.Get_HFA11()), TestMan.EXPECTED_SUM_HFA11) ? 0 : 1;
            nFailures += Common.CheckResult("Get HFA 19", TestMan.Sum_HFA19(TestMan.Get_HFA19()), TestMan.EXPECTED_SUM_HFA19) ? 0 : 1;

            return nFailures == 0 ? Common.SUCC_RET_CODE : Common.FAIL_RET_CODE;
        }
    }
}
