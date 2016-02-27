// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Convert.ToString(System.Int64)
/// </summary>
public class ConvertToString19
{
    public static int Main()
    {
        ConvertToString19 testObj = new ConvertToString19();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToString(System.Int64)");
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

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest1: Verify value is a random Int64... ";
        string c_TEST_ID = "P001";


        Int64 intValue = TestLibrary.Generator.GetInt64(-55);

        String actualValue = intValue.ToString();
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(intValue);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue + " as expected: Actual is " + actualValue;
                errorDesc += "\n Int64 value is " + intValue;
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
        string c_TEST_DESC = "PosTest2: Verify value is Int64.MaxValue... ";
        string c_TEST_ID = "P002";


        Int64 intValue = Int64.MaxValue;

        String actualValue = "9223372036854775807";
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(intValue);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue + " as expected: Actual is " + actualValue;
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
        string c_TEST_DESC = "PosTest3: Verify value is Int64.MinValue... ";
        string c_TEST_ID = "P003";


        Int64 intValue = Int64.MinValue;

        String actualValue = "-9223372036854775808";
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(intValue);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue + " as expected: Actual is " + actualValue;
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
    #endregion

   
}
