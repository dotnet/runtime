// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// String.Replace(String, String)
/// Replaces all occurrences of a specified Unicode character in this instance 
/// with another specified Unicode character. 
/// </summary>
public class StringReplace2
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    private const int c_MAX_SHORT_STR_LEN = 31;//short string (<32 chars)
    private const int c_MIN_LONG_STR_LEN = 257;//long string ( >256 chars)
    private const int c_MAX_LONG_STR_LEN = 65535;

    public static int Main()
    {
        StringReplace2 sr = new StringReplace2();

        TestLibrary.TestFramework.BeginTestCase("for method: System.String.Replace(String, String)");
        if(sr.RunTests())
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
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive test scenarios

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Source string contains old string.";
        const string c_TEST_ID = "P001";

        string strSrc;
        string oldStr, newStr;
        bool condition;
        bool expectedValue = true;
        bool actualValue = false;

        //Initialize the parameters
        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        int startIndex;
        startIndex = GetInt32(0, strSrc.Length - 1);
        oldStr = strSrc.Substring(startIndex, GetInt32(1, strSrc.Length - startIndex));
        newStr = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string strReplaced = strSrc.Replace(oldStr, newStr);

            string subStrOld, subStrNew;
            int oldIndex = 0;
            int newIndex;
            int maxValidIndex = strReplaced.Length - newStr.Length;
            condition = true;
            // compare string replaed and source string
            for (newIndex = 0; newIndex < maxValidIndex; )
            {
                subStrNew = strReplaced.Substring(newIndex, newStr.Length);

                if(0 == string.CompareOrdinal(subStrNew, newStr))
                {
                    subStrOld = strSrc.Substring(oldIndex, oldStr.Length);
                    condition = (0 == string.CompareOrdinal(subStrOld, oldStr)) && condition;
                    oldIndex += oldStr.Length;
                    newIndex += newStr.Length;
                }
                else
                {
                    condition = (strReplaced[newIndex] == strSrc[oldIndex]) && condition;
                    oldIndex++;
                    newIndex++;
                }
            }

            actualValue = condition;
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, oldStr, newStr);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldStr, newStr));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: Source string does not contain old string.";
        const string c_TEST_ID = "P002";

        string strSrc;
        string oldStr, newStr;
        bool expectedValue = true;
        bool actualValue = false;

        //Initialize the parameters
        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        oldStr = strSrc + TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        newStr = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string strReplaced = strSrc.Replace(oldStr, newStr);

            actualValue = (0 == string.CompareOrdinal(strSrc, strReplaced));
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, oldStr, newStr);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldStr, newStr));
            retVal = false;
        }

        return retVal;
    }

    //new update added by Noter(v-yaduoj) 8-10-2006
    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest3: Source string contains old string, replace with \"\\0\"";
        const string c_TEST_ID = "P003";

        string strSrc;
        string oldStr, newStr;
        bool condition;
        bool expectedValue = true;
        bool actualValue = false;

        //Initialize the parameters
        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        int startIndex;
        startIndex = GetInt32(0, strSrc.Length - 1);
        oldStr = strSrc.Substring(startIndex, GetInt32(1, strSrc.Length - startIndex));
        newStr = "\0";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string strReplaced = strSrc.Replace(oldStr, newStr);

            string subStrOld, subStrNew;
            int oldIndex = 0;
            int newIndex;
            int maxValidIndex = strReplaced.Length - newStr.Length;
            condition = true;
            // compare string replaed and source string
            for (newIndex = 0; newIndex < maxValidIndex; )
            {
                subStrNew = strReplaced.Substring(newIndex, newStr.Length);

                if (0 == string.CompareOrdinal(subStrNew, newStr))
                {
                    subStrOld = strSrc.Substring(oldIndex, oldStr.Length);
                    condition = (0 == string.CompareOrdinal(subStrOld, oldStr)) && condition;
                    oldIndex += oldStr.Length;
                    newIndex += newStr.Length;
                }
                else
                {
                    condition = (strReplaced[newIndex] == strSrc[oldIndex]) && condition;
                    oldIndex++;
                    newIndex++;
                }
            }

            actualValue = condition;
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, oldStr, newStr);
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldStr, newStr));
            retVal = false;
        }

        return retVal;
    }

    #endregion

    #region Negative test scenarios

    //ArgumentNullException
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest1: old string value is null reference.";
        const string c_TEST_ID = "N001";

        string strSrc;
        string oldStr, newStr;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        oldStr = null;
        newStr = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try 
        {
            strSrc.Replace(oldStr, newStr);
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, "ArgumentNullException is not thrown as expected." + GetDataString(strSrc, oldStr, newStr));
            retVal = false;
        }
        catch (ArgumentNullException)
        {}
        catch(Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldStr, newStr));
            retVal = false;
        }

        return retVal;
    }

    //ArgumentException
    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest2: old string value is String.Empty";
        const string c_TEST_ID = "N002";

        string strSrc;
        string oldStr, newStr;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        oldStr = String.Empty;
        newStr = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            strSrc.Replace(oldStr, newStr);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected." + GetDataString(strSrc, oldStr, newStr));
            retVal = false;
        }
        catch (ArgumentException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldStr, newStr));
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

    private string GetDataString(string strSrc, string oldStr, string newStr)
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
        str += string.Format("\n[Old string]\n{0}", oldStr);
        str += string.Format("\n[New string]\n{0}", newStr);

        return str;
    }
}
