// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// String.Replace(Char, Char)
/// Replaces all occurrences of a specified Unicode character in this instance 
/// with another specified Unicode character. 
/// </summary>
public class StringReplace1
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    private const int c_MAX_SHORT_STR_LEN = 31;//short string (<32 chars)
    private const int c_MIN_LONG_STR_LEN = 257;//long string ( >256 chars)
    private const int c_MAX_LONG_STR_LEN = 65535;

    public static int Main()
    {
        StringReplace1 sr = new StringReplace1();

        TestLibrary.TestFramework.BeginTestCase("for method: System.String.Replace(Int32, Int32)");
        if (sr.RunTests())
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

    #region Positive test scenarios

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Old char exists in source string, random new char.";
        const string c_TEST_ID = "P001";

        string strSrc;
        char oldChar, newChar;
        bool condition1 = false; //Verify the length invariant
        bool condition2 = false; //Verify to replace correctly
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        //New update: modified by Noter(v-yaduoj) 8-10-2006
        oldChar = strSrc[TestLibrary.Generator.GetInt32(-55) % (strSrc.Length)];
        newChar = TestLibrary.Generator.GetChar(-55);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string strReplaced = strSrc.Replace(oldChar, newChar);

            condition1 = (strReplaced.Length == strSrc.Length);
            if (condition1)
            {
                condition2 = true;
                for (int i = 0; i < strReplaced.Length; i++)
                {
                    //Delete the incorrect check logic
                    // new update 8-10-2006, Noter(v-yaduoj)
                    if (strSrc[i] == oldChar)
                    {
                        condition2 = (strReplaced[i] == newChar) && condition2;
                    }
                    else
                    {
                        condition2 = (strSrc[i] == strReplaced[i]) && condition2;
                    }
                } // end for statement
            } // end if

            actualValue = condition1 && condition2;
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, oldChar, newChar);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldChar, newChar));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: Old char does not exist in source string, random new char.";
        const string c_TEST_ID = "P002";

        string strSrc;
        char oldChar, newChar;
        bool expectedValue = true;
        bool actualValue = false;

        //New update: modified by Noter(v-yaduoj) 8-10-2006
        oldChar = TestLibrary.Generator.GetChar(-55);
        newChar = TestLibrary.Generator.GetChar(-55);

        //Generate source string does not contain old char
        int length = GetInt32(c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        int index = 0;
        char ch;
        strSrc = string.Empty;
        while (index < length)
        {
            ch = TestLibrary.Generator.GetChar(-55);
            if (oldChar == ch)
            {
                continue;
            }
            strSrc += ch.ToString();
            index++;
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string strReplaced = strSrc.Replace(oldChar, newChar);

            //New update: modified by Noter(v-yaduoj) 8-10-2006
            actualValue = (0 == string.CompareOrdinal(strReplaced, strSrc));
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, oldChar, newChar);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldChar, newChar));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = @"PosTest3: Old char exists in source string, new char is \0 ";
        const string c_TEST_ID = "P003";

        string strSrc;
        char oldChar, newChar;
        bool condition1 = false; //Verify the length invariant
        bool condition2 = false; //Verify to replace correctly
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        oldChar = strSrc[TestLibrary.Generator.GetInt32(-55) % strSrc.Length];
        newChar = '\0';

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string strReplaced = strSrc.Replace(oldChar, newChar);

            condition1 = (strReplaced.Length == strSrc.Length);
            if (condition1)
            {
                condition2 = true;
                for (int i = 0; i < strReplaced.Length; i++)
                {
                    //Delete the incorrect check logic
                    // new update 8-10-2006, Noter(v-yaduoj)
                    if (strSrc[i] == oldChar)
                    {
                        condition2 = (strReplaced[i] == newChar) && condition2;
                    }
                    else
                    {
                        condition2 = (strSrc[i] == strReplaced[i]) && condition2;
                    }
                }// end for
            } // end if

            actualValue = condition1 && condition2;
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, oldChar, newChar);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldChar, newChar));
            retVal = false;
        }

        return retVal;
    }

    //Zero weight character '\u0400'
    // new update 8-10-2006, Noter(v-yaduoj)
    public bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_DESC = @"PosTest4: Old char '\u0400' is zero weight character, random new char";
        const string c_TEST_ID = "P004";

        string strSrc;
        char oldChar, newChar;
        bool condition1 = false; //Verify the length invariant
        bool condition2 = false; //Verify to replace correctly
        bool expectedValue = true;
        bool actualValue = false;

        oldChar = '\u0400';
        newChar = TestLibrary.Generator.GetChar(-55);

        //Generate source string contains '\u0400'
        int length = GetInt32(c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        char[] chs = new char[length];

        strSrc = oldChar.ToString();
        for (int i, index = 1; index < length; index++)
        {
            i = TestLibrary.Generator.GetInt32(-55) % 6;
            if (4 == i)
            {
                strSrc += oldChar.ToString();
            }
            else
            {
                strSrc += TestLibrary.Generator.GetChar(-55);
            }
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string strReplaced = strSrc.Replace(oldChar, newChar);

            condition1 = (strReplaced.Length == strSrc.Length);
            if (condition1)
            {
                condition2 = true;
                for (int i = 0; i < strReplaced.Length; i++)
                {
                    if (strSrc[i] == oldChar)
                    {
                        condition2 = (strReplaced[i] == newChar) && condition2;
                    }
                    else
                    {
                        condition2 = (strSrc[i] == strReplaced[i]) && condition2;
                    }
                }// end for
            } // end if

            actualValue = condition1 && condition2;
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, oldChar, newChar);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldChar, newChar));
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

    private string GetDataString(string strSrc, char oldChar, char newChar)
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
        str += string.Format("\n[Old char]\n{0}", oldChar);
        str += string.Format("\n[New char]\n{0}", newChar);

        return str;
    }
}
