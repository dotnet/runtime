// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Convert.ToSingle(System.UInt64)
/// </summary>
public class ConvertToSingle17
{
    public static int Main()
    {
        ConvertToSingle17 testObj = new ConvertToSingle17();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToSingle(System.UInt64)");
        if (testObj.RunTests())
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

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest1: Verfify value is a random UInt64... ";
        string c_TEST_ID = "P001";

        UInt64 actualValue = GetUInt64(UInt64.MinValue, UInt64.MaxValue);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Single resValue = Convert.ToSingle(actualValue);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest2: Verfify value is UInt64.MaxValue... ";
        string c_TEST_ID = "P002";

        UInt64 actualValue = UInt64.MaxValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Single resValue = Convert.ToSingle(actualValue);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest3: Verify value is UInt64.MinValue... ";
        string c_TEST_ID = "P003";

        UInt64 actualValue = UInt64.MinValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Single resValue = Convert.ToSingle(actualValue);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest4: Verfify value is zero... ";
        string c_TEST_ID = "P004";

        UInt64 actualValue = 0;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Single resValue = Convert.ToSingle(actualValue);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Help Method
    private UInt64 GetUInt64(UInt64 minValue, UInt64 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                Random rand = new Random(-55);
                int i = rand.Next(0, 2);
                return minValue + ((UInt64)(TestLibrary.Generator.GetInt64(-55)*2+i))%(maxValue -minValue);
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }
    #endregion
}
