// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Decimal.ctor(System.Int64)
/// </summary>
public class DecimalCtor5
{

    public static int Main()
    {
        DecimalCtor5 dCtor5 = new DecimalCtor5();
        TestLibrary.TestFramework.BeginTestCase("for Constructor:System.Decimal.Ctor(System.Int64)");

        if (dCtor5.RunTests())
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
        retVal = PosTest4() && retVal;

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1:Verify the param is a random Int64... ";
        const string c_TEST_ID = "P001";

        long value = TestLibrary.Generator.GetInt64(-55);


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = new Decimal(value);

            if (decimalValue != Convert.ToDecimal(value))
            {
                string errorDesc = "Value is not " + Convert.ToDecimal(value).ToString() + " as expected: param is " + decimalValue.ToString();
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
           

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2:Verify the param is zero... ";
        const string c_TEST_ID = "P002";

        long value = 0;


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = new Decimal(value);

            if (decimalValue != Convert.ToDecimal(value))
            {
                string errorDesc = "Value is not " + Convert.ToDecimal(value).ToString() + " as expected: param is " + decimalValue.ToString();
                TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest3:Verify the param is long.MaxValue... ";
        const string c_TEST_ID = "P003";

        long value = long.MaxValue;


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = new Decimal(value);

            if (decimalValue != Convert.ToDecimal(value))
            {
                string errorDesc = "Value is not " + Convert.ToDecimal(value).ToString() + " as expected: param is " + decimalValue.ToString();
                TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest4:Verify the param is long.MinValue... ";
        const string c_TEST_ID = "P004";

        long value = long.MinValue;


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = new Decimal(value);

            if (decimalValue != Convert.ToDecimal(value))
            {
                string errorDesc = "Value is not " + Convert.ToDecimal(value).ToString() + " as expected: param is " + decimalValue.ToString();
                TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }
    #endregion
}
