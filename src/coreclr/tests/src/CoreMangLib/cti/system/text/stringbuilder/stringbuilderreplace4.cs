// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Text.StringBuilder.Replace(oldString,newString,startIndex,count)
/// </summary>
class StringBuilderReplace4
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    private const int c_MAX_SHORT_STR_LEN = 31;//short string (<32 chars)
    private const int c_MIN_LONG_STR_LEN = 257;//long string ( >256 chars)
    private const int c_MAX_LONG_STR_LEN = 65535;

    public static int Main()
    {
        StringBuilderReplace4 test = new StringBuilderReplace4();

        TestLibrary.TestFramework.BeginTestCase("for Method:System.Text.StringBuilder.Replace(oldString,newString,startIndex,count)");

        if (test.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Old string exists in source StringBuilder, random new string. ";
        const string c_TEST_ID = "P001";


        string strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(strSrc);

        string newString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        int subStartIndex = TestLibrary.Generator.GetInt32(-55) % strSrc.Length;
        int subLength = TestLibrary.Generator.GetInt32(-55) % (strSrc.Length - subStartIndex);
        while (subLength == 0)
        {
            subStartIndex = TestLibrary.Generator.GetInt32(-55) % strSrc.Length;
            subLength = TestLibrary.Generator.GetInt32(-55) % (strSrc.Length - subStartIndex);
        }
        string oldString = strSrc.Substring(subStartIndex, subLength);
        System.Text.StringBuilder newStringBuilder = new System.Text.StringBuilder(strSrc.Replace(oldString, newString));

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            stringBuilder.Replace(oldString, newString, 0, strSrc.Length);
            if (stringBuilder.ToString() != newStringBuilder.ToString())
            {
                string errorDesc = "Value is not " + newStringBuilder.ToString() + " as expected: Actual(" + stringBuilder.ToString() + ")";
                errorDesc += GetDataString(strSrc, oldString, newString,0,strSrc.Length);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldString, newString, 0, strSrc.Length));
            retVal = false;  
        }


        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2: the source StringBuilder is empty. ";
        const string c_TEST_ID = "P002";

        string strSrc = string.Empty;
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(strSrc);

        string newString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        string oldString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN,c_MAX_STRING_LEN);
        System.Text.StringBuilder newStringBuilder = new System.Text.StringBuilder(strSrc.Replace(oldString, newString));

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            stringBuilder.Replace(oldString, newString, 0, strSrc.Length);
            if (stringBuilder.ToString() != newStringBuilder.ToString())
            {
                string errorDesc = "Value is not " + newStringBuilder.ToString() + " as expected: Actual(" + stringBuilder.ToString() + ")";
                errorDesc += GetDataString(strSrc, oldString, newString, 0, strSrc.Length);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldString, newString, 0, strSrc.Length));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest3: Old string is random, new string is randow also. ";
        const string c_TEST_ID = "P003";

        string strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(strSrc);

        string newString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        string oldString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        System.Text.StringBuilder newStringBuilder = new System.Text.StringBuilder(strSrc.Replace(oldString, newString));

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            stringBuilder.Replace(oldString, newString, 0, strSrc.Length);
            if (stringBuilder.ToString() != newStringBuilder.ToString())
            {
                string errorDesc = "Value is not " + newStringBuilder.ToString() + " as expected: Actual(" + stringBuilder.ToString() + ")";
                errorDesc += GetDataString(strSrc, oldString, newString, 0, strSrc.Length);
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldString, newString, 0, strSrc.Length));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest4: Old string is out of range of the source StingBuilder. ";
        const string c_TEST_ID = "P004";

        string strSrc = "We always believe that the old time is good time and  future time is bad time";
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(strSrc);

        string newString = "thing";
        string oldString = "time";
        string replacedString = "We always believe that the old thing is good thing and  future time is bad time";
        System.Text.StringBuilder newStringBuilder = new System.Text.StringBuilder(replacedString);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            stringBuilder.Replace(oldString, newString, 2, 47);
            if (stringBuilder.ToString() != newStringBuilder.ToString())
            {
                string errorDesc = "Value is not " + newStringBuilder.ToString() + " as expected: Actual(" + stringBuilder.ToString() + ")";
                errorDesc += GetDataString(strSrc, oldString, newString, 2, 47);
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldString, newString, 2, 47));
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region NegativeTesting
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest1: Start index is  larger than source StringBuilder's length";
        const string c_TEST_ID = "N001";

        string strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(strSrc);

        int startIndex = TestLibrary.Generator.GetInt32(-55) + stringBuilder.Length;
        int count = TestLibrary.Generator.GetInt32(-55);
        string newString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        string oldString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            stringBuilder.Replace(oldString, newString, startIndex, count);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected." + GetDataString(strSrc, oldString, newString, startIndex, count));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldString, newString, startIndex, count));
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest2: startIndex plus count indicates a character position not within the source StringBuilder";
        const string c_TEST_ID = "N002";

        string strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(strSrc);

        int startIndex = TestLibrary.Generator.GetInt32(-55) % strSrc.Length;
        int count = TestLibrary.Generator.GetInt32(-55)+stringBuilder.Length-startIndex;
        string newString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        string oldString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            stringBuilder.Replace(oldString, newString, startIndex, count);
            TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected." + GetDataString(strSrc, oldString, newString, stringBuilder.Length, stringBuilder.Length));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldString, newString, startIndex, count));
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest3: old string  is a null reference ";
        const string c_TEST_ID = "N003";

        string strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(strSrc);

        int startIndex = TestLibrary.Generator.GetInt32(-55) % strSrc.Length;
        int count = stringBuilder.Length - startIndex-1;
        string newString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        string oldString = null;


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            stringBuilder.Replace(oldString, newString, startIndex,count);
            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected." + GetDataString(strSrc, oldString, newString, stringBuilder.Length, stringBuilder.Length));
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldString, newString, startIndex, count));
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest4: old string  is empty ";
        const string c_TEST_ID = "N004";

        string strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(strSrc);

        int startIndex = TestLibrary.Generator.GetInt32(-55) % strSrc.Length;
        int count = stringBuilder.Length - startIndex - 1;
        string newString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        string oldString = string.Empty;


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            stringBuilder.Replace(oldString, newString, startIndex, count);
            TestLibrary.TestFramework.LogError("015" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected." + GetDataString(strSrc, oldString, newString, stringBuilder.Length, stringBuilder.Length));
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc,oldString, newString, startIndex, count));
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest5: the source StringBuilder is null reference";
        const string c_TEST_ID = "N005";

        System.Text.StringBuilder stringBuilder = null;

        int startIndex = 0;
        int count = 0;
        string newString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        string oldString = string.Empty;


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            stringBuilder.Replace(oldString, newString, startIndex, count);
            TestLibrary.TestFramework.LogError("017" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected." + GetDataString(null,oldString, newString, stringBuilder.Length, stringBuilder.Length));
            retVal = false;
        }
        catch (NullReferenceException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(null,oldString, newString, startIndex, count));
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Helper methords for testing
    private string GetDataString(string strSrc, string oldString, string newString, int startIndex, int count)
    {
        string str1, str,oldStr,newStr;
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

        if (null == oldString)
        {
            oldStr = "null";
        }
        else
        {
            oldStr = oldString;
        }


        if (null == newString)
        {
            newStr = newString;
        }
        else 
        {
            newStr = newString;
        }

        str = string.Format("\n[Source StingBulider value]\n \"{0}\"", str1);
        str += string.Format("\n[Length of source StringBuilder]\n {0}", len1);
        str += string.Format("\n[Start index ]\n{0}", startIndex);
        str += string.Format("\n[Replace count]\n{0}", count);
        str += string.Format("\n[Old string]\n{0}", oldStr);
        str += string.Format("\n[New string]\n{0}", newStr);

        return str;
    }
    #endregion

}
