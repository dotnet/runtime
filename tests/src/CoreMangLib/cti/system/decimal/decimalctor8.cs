// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Decimal.ctor(System.UInt64)
/// </summary>
public class DecimalCtor8
{

    public static int Main()
    {
        DecimalCtor8 dCtor8 = new DecimalCtor8();
        TestLibrary.TestFramework.BeginTestCase("for Constructor:System.Decimal.Ctor(System.UInt64)");

        if (dCtor8.RunTests())
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("FAIL");
            return 0;
        }
    }
    public bool RunTests()
    {
        bool retVal = true;
        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1:Verify the param is a random UInt64 ";
        const string c_TEST_ID = "P001";

        System.UInt64 uint64Value = Convert.ToUInt64(TestLibrary.Generator.GetInt64(-55));

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = new Decimal(uint64Value);
            if (decimalValue != Convert.ToDecimal(uint64Value))
            {
                string errorDesc = "Value is not " + Convert.ToDecimal(uint64Value).ToString() + " as expected: param is " + decimalValue.ToString();
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2:Verify the param is UInt64.MinValue(0) ";
        const string c_TEST_ID = "P002";

        UInt64 dValue = UInt64.MinValue;
        Decimal resValue = 0m;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = new Decimal(dValue);
            if (decimalValue != resValue)
            {
                string errorDesc = "Value is not " + resValue.ToString() + " as expected: param is " + decimalValue.ToString();
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest3:Verify the param is UInt64.MaxValue ";
        const string c_TEST_ID = "P003";

        UInt64 dValue = UInt64.MaxValue;
        Decimal resValue = 18446744073709551615m;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = new Decimal(dValue);
            if (decimalValue != resValue)
            {
                string errorDesc = "Value is not " + resValue.ToString() + " as expected: param is " + decimalValue.ToString();
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }
    #endregion

}
