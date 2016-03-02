// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
/// <summary>
/// String.IsNullOrEmpty(System.String)
/// Note: This method is new in the .NET Framework version 2.0. 
/// Indicates whether the specified String object is a null refere-
/// nce (Nothing in Visual Basic) or an Empty string. 
/// </summary>
class StringIsNullOrEmpty
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    private const int c_MAX_SHORT_STR_LEN = 31;//short string (<32 chars)
    private const int c_MIN_LONG_STR_LEN = 257;//long string ( >256 chars)
    private const int c_MAX_LONG_STR_LEN = 65535;

    public static int Main()
    {
        StringIsNullOrEmpty sin = new StringIsNullOrEmpty();

        TestLibrary.TestFramework.BeginTestCase("for method: System.String.IsNullOrEmpty(System.String)");
        if(sin.RunTests())
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
        retVal = PosTest5() && retVal;

        return retVal;
    }

    #region Positive scenarios

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "String is null";
        const string c_TEST_ID = "P001";

        string str;
        bool expectedValue = true; // True means that string is null.
        bool actualValue = false;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            str = null;
            actualValue = string.IsNullOrEmpty(str);
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("001" + "TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "String is empty";
        const string c_TEST_ID = "P002";

        string str;
        bool expectedValue = true; // True means string is empty
        bool actualValue = false;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            str = String.Empty;
            actualValue = string.IsNullOrEmpty(str);
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("003" + "TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "String is unempty and non-null";
        const string c_TEST_ID = "P003";

        string str;
        bool expectedValue = false; // False means string is unempty and non-null
        bool actualValue = false;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            str = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            actualValue = string.IsNullOrEmpty(str);
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("005" + "TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_DESC = "String consists of \"\\0\"";
        const string c_TEST_ID = "P004";

        string str;
        bool expectedValue = false; // False means string is unempty and non-null
        bool actualValue = false;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            char ch = '\0';
            str = new string(ch, GetInt32(1, c_MAX_STRING_LEN));
            actualValue = string.IsNullOrEmpty(str);
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("007" + "TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        const string c_TEST_DESC = "String consists of \"\"";
        const string c_TEST_ID = "P005";

        string str;
        bool expectedValue = true;
        bool actualValue = false;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            str = "";
            for (int i = GetInt32(1, c_MAX_STRING_LEN); i > 0; i--)
            {
                str += "";
            }
            actualValue = string.IsNullOrEmpty(str);
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("009" + "TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e);
            retVal = false;
        }

        return retVal;
    }

    #endregion

    #region helper methods for generating test data

    private bool GetBoolean()
    {
        Int32 i = this.GetInt32(1, 2);
        return (i == 1) ? true : false;
    }

    //Get a non-negative integer between minValue and maxValue
    private Int32 GetInt32(Int32 minValue, Int32 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return minValue + TestLibrary.Generator.GetInt32(-55) % (maxValue - minValue);
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }

    private Int32 Min(Int32 i1, Int32 i2)
    {
        return (i1 <= i2) ? i1 : i2;
    }

    private Int32 Max(Int32 i1, Int32 i2)
    {
        return (i1 >= i2) ? i1 : i2;
    }

    #endregion
}
