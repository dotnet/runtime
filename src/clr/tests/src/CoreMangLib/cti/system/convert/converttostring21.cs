// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
using System.Runtime.CompilerServices;

/// <summary>
/// Convert.ToString(System.Int64,System.Int32)
/// </summary>
public class ConvertToString21
{
    public static int Main()
    {
        ConvertToString21 testObj = new ConvertToString21();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToString(System.Int64,System.Int32)");
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest1: Verify value is -123 and radix is 2,8,10 or 16... ";
        string c_TEST_ID = "P001";


        Int64 int64Value = -123;
        int radix;
        String actualValue;
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            actualValue = "1111111111111111111111111111111111111111111111111111111110000101";
            radix = 2;
            String resValue = Convert.ToString(int64Value, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(int64Value, radix);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            radix = 8;
            actualValue = "1777777777777777777605";
            errorDesc = "";
            resValue = Convert.ToString(int64Value, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(int64Value, radix);
                TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            radix = 10;
            actualValue = "-123";
            errorDesc = "";
            resValue = Convert.ToString(int64Value, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(int64Value, radix);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            radix = 16;
            actualValue = "ffffffffffffff85";
            errorDesc = "";
            resValue = Convert.ToString(int64Value, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(int64Value, radix);
                TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest2: Verify value is Int64.MaxValue and radix is 2,8,10 or 16... ";
        string c_TEST_ID = "P002";

        Int64 int64Value = Int64.MaxValue;
        int radix;
        String actualValue;
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            actualValue = "111111111111111111111111111111111111111111111111111111111111111";
            radix = 2;
            String resValue = Convert.ToString(int64Value, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(int64Value, radix);
                TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            radix = 8;
            actualValue = "777777777777777777777";
            errorDesc = "";
            resValue = Convert.ToString(int64Value, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(int64Value, radix);
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            radix = 10;
            actualValue = "9223372036854775807";
            errorDesc = "";
            resValue = Convert.ToString(int64Value, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(int64Value, radix);
                TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            radix = 16;
            actualValue = "7fffffffffffffff";
            errorDesc = "";
            resValue = Convert.ToString(int64Value, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(int64Value, radix);
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest3: Verify value is Int64.MinValue and radix is 2,8,10 or 16... ";
        string c_TEST_ID = "P003";

        Int64 int64Value = Int64.MinValue;
        int radix;
        String actualValue;
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            actualValue = "1000000000000000000000000000000000000000000000000000000000000000";
            radix = 2;
            String resValue = Convert.ToString(int64Value, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(int64Value, radix);
                TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            radix = 8;
            actualValue = "1000000000000000000000";
            errorDesc = "";
            resValue = Convert.ToString(int64Value, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(int64Value, radix);
                TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            radix = 10;
            actualValue = "-9223372036854775808";
            errorDesc = "";
            resValue = Convert.ToString(int64Value, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(int64Value, radix);
                TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            radix = 16;
            actualValue = "8000000000000000";
            errorDesc = "";
            resValue = Convert.ToString(int64Value, radix);

            if (actualValue != resValue)
            {
                errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += DataString(int64Value, radix);
                TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("015", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region NegativeTesting
    public bool NegTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest1: the radix is 32...";
        const string c_TEST_ID = "N001";

        Int32 int32Value = TestLibrary.Generator.GetInt32(-55);
        int radix = 32;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Convert.ToString(int32Value, radix);
            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected ." + DataString(int32Value, radix));
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + DataString(int32Value, radix));
            retVal = false;
        }

        return retVal;

    }
    #endregion

    #region Help Methods
    private string DataString(Int64 int64Value, int radix)
    {
        string str;

        str = string.Format("\n[int64Value value]\n \"{0}\"", int64Value);
        str += string.Format("\n[radix value ]\n {0}", radix);

        return str;
    }
    #endregion
}
