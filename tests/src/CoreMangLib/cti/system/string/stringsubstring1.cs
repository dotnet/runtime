// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// String.SubString(Int32)
/// Retrieves a substring from this instance. The substring starts at a specified character position.  
/// </summary>
public class StringSubString1
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    private const int c_MAX_SHORT_STR_LEN = 31;//short string (<32 chars)
    private const int c_MIN_LONG_STR_LEN = 257;//long string ( >256 chars)
    private const int c_MAX_LONG_STR_LEN = 65535;

    public static int Main()
    {
        StringSubString1 sss = new StringSubString1();

        TestLibrary.TestFramework.BeginTestCase("for method: System.String.SubString(Int32)");
        if(sss.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive test scenarios

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Start index is valid random value.";
        const string c_TEST_ID = "P001";

        string strSrc;
        int startIndex;
        bool condition = false;
        bool expectedValue = true;
        bool actualValue = false;

        //Initialize the parameters
        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        startIndex = TestLibrary.Generator.GetInt32(-55) % (strSrc.Length);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string subStr = strSrc.Substring(startIndex);

            condition = true;
            for (int i = 0; i < subStr.Length; i++)
            {
                condition = (subStr[i] == strSrc[i + startIndex]) && condition;
            }

            actualValue = condition;
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, startIndex);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, startIndex));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: startIndex equals the length of this instance.";
        const string c_TEST_ID = "P002";

        string strSrc;
        int startIndex;
        bool condition = false;
        bool expectedValue = true;
        bool actualValue = false;

        //Initialize the parameters
        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        startIndex = strSrc.Length;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string subStr = strSrc.Substring(startIndex);
            condition = (0 == String.CompareOrdinal(string.Empty, subStr));
            actualValue = condition;
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, startIndex);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, startIndex));
            retVal = false;
        }

        return retVal;
    }

    #endregion

    #region Negative test scenarios

    //ArgumentOutOfRangeException
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest1: Start index is negative";
        const string c_TEST_ID = "N001";

        string strSrc;
        int startIndex;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        startIndex = -1 * TestLibrary.Generator.GetInt32(-55) - 1;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try 
        {
            strSrc.Substring(startIndex);
            TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected." + GetDataString(strSrc, startIndex));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {}
        catch(Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, startIndex));
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest2: Start index is greater than string length";
        const string c_TEST_ID = "N002";

        string strSrc;
        int startIndex;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        startIndex = GetInt32(strSrc.Length + 1, Int32.MaxValue);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            strSrc.Substring(startIndex);
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected." + GetDataString(strSrc, startIndex));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, startIndex));
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

    private string GetDataString(string strSrc, int startIndex)
    {
        string str1, str;
        int len1;

        if (null == strSrc)
        {
            str1 = "null";
            len1 = 0;
        }
        else
        {
            str1 = strSrc;
            len1 = strSrc.Length;
        }

        str = string.Format("\n[Source string value]\n \"{0}\"", str1);
        str += string.Format("\n[Length of source string]\n {0}", len1);
        str += string.Format("\n[Start index]\n{0}", startIndex);

        return str;
    }
}
