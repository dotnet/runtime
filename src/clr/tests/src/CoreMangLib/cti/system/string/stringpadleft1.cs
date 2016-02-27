// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// String.PadLeft(Int32)  
/// Right-aligns the characters in this instance, 
/// padding with spaces on the left for a specified total length
/// </summary>
public class StringPadLeft1
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    private const int c_MAX_SHORT_STR_LEN = 31;//short string (<32 chars)
    private const int c_MIN_LONG_STR_LEN = 257;//long string ( >256 chars)
    private const int c_MAX_LONG_STR_LEN = 65535;

    public static int Main()
    {
        StringPadLeft1 spl = new StringPadLeft1();

        TestLibrary.TestFramework.BeginTestCase("for method: System.String.PadLeft(Int32)");
        if (spl.RunTests())
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

        const string c_TEST_DESC = "PosTest1: Total width is greater than old string length";
        const string c_TEST_ID = "P001";

        int totalWidth;
        string str;
        bool condition1 = false; //Verify the space paded
        bool condition2 = false; //Verify the old string
        bool expectedValue = true;
        bool actualValue = false;

        str = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        //str = "hello";
        totalWidth = GetInt32(str.Length + 1, str.Length + c_MAX_STRING_LEN);
        //totalWidth = 8;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string strPaded = str.PadLeft(totalWidth);
            
            char[] trimChs = new char[] {'\x0020'};
            string spaces = new string('\x0020', totalWidth - str.Length);

            string spacesPaded = strPaded.Substring(0, totalWidth - str.Length);
            condition1 = (string.CompareOrdinal(spaces, spacesPaded) == 0);
            condition2 = (string.CompareOrdinal(strPaded.TrimStart(trimChs), str) == 0);
            actualValue = condition1 && condition2;
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(str, totalWidth);
                TestLibrary.TestFramework.LogError("001" + "TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e + GetDataString(str, totalWidth));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2:  0 <= total width <= old string length";
        const string c_TEST_ID = "P002";

        int totalWidth;
        string str;
        bool expectedValue = true;
        bool actualValue = false;

        str = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        totalWidth = GetInt32(0, str.Length - 1);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string strPaded = str.PadLeft(totalWidth);
            actualValue = (0 == string.CompareOrdinal(strPaded, str));
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(str, totalWidth);
                TestLibrary.TestFramework.LogError("003" + "TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e + GetDataString(str, totalWidth));
            retVal = false;
        }

        return retVal;
    }

    #endregion

    #region Negative test scenarios
    
    //ArgumentException
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest1: Total width is less than zero. ";
        const string c_TEST_ID = "N001";

        int totalWidth;
        string str;

        totalWidth = -1 * TestLibrary.Generator.GetInt32(-55) - 1;
        str = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            str.PadLeft(totalWidth);
            TestLibrary.TestFramework.LogError("005" + "TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected" + GetDataString(str, totalWidth));
            retVal = false;
        }
        catch (ArgumentException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e +GetDataString(str, totalWidth));
            retVal = false;
        }

        return retVal;
    }

    //OutOfMemoryException
    public bool NegTest2() // bug 8-8-2006 Noter(v-yaduoj)
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest2: Too great width ";
        const string c_TEST_ID = "N002";

        int totalWidth;
        string str;

        totalWidth = Int32.MaxValue;
        str = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            str.PadLeft(totalWidth);
            TestLibrary.TestFramework.LogError("007" + "TestId-" + c_TEST_ID, "OutOfMemoryException is not thrown as expected" + GetDataString(str, totalWidth));
            retVal = false;
        }
        catch (OutOfMemoryException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e + GetDataString(str, totalWidth));
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

    private string GetDataString(string strSrc, int totalWidth)
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
        str += string.Format("\n[Total width]\n{0}", totalWidth);

        return str;
    }

}
