// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Text.StringBuilder.Replace(char,char)
/// </summary>
public class StringBuilderReplace1
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    private const int c_MAX_SHORT_STR_LEN = 31;//short string (<32 chars)
    private const int c_MIN_LONG_STR_LEN = 257;//long string ( >256 chars)
    private const int c_MAX_LONG_STR_LEN = 65535;

    public static int Main()
    {
        StringBuilderReplace1 test = new StringBuilderReplace1();

        TestLibrary.TestFramework.BeginTestCase("for Method:System.Text.StringBuilder.Replace(char1,char2)");

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

        return retVal;
    }
    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Verify StringBuilder Replace char of random ");

        string oldString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
        stringBuilder.Append(oldString);
        
        char oldChar = TestLibrary.Generator.GetChar(-55);
        char newChar = TestLibrary.Generator.GetChar(-55);

        while (0 == oldChar.CompareTo(newChar))
        {
            newChar = TestLibrary.Generator.GetChar(-55);
        }

        int indexChar = oldString.IndexOf(oldChar);

        string replacedSrc = oldString.Replace(oldChar, newChar);

        try
        {
            if (-1 != indexChar)
            {
                stringBuilder.Replace(oldChar, newChar);
                if (0 != string.CompareOrdinal(stringBuilder.ToString(),replacedSrc))
                {
                    TestLibrary.TestFramework.LogError("001", "StringBuilder\"" + oldString + "\" can't corrently Replace \"" + oldChar.ToString() + "\" to \"" + newChar + "\" ");
                    retVal = false;
                }
            }
            else
            {
                stringBuilder.Replace(oldChar, newChar);
                if (-1 != stringBuilder.ToString().IndexOf(oldChar))
                {
                    TestLibrary.TestFramework.LogError("001", "StringBuilder\"" + oldString + "\" can't corrently Replace \"" + oldChar.ToString() + "\" to \"" + newChar + "\" ");
                    retVal = false;
                }
            }
        }
        catch(Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Verify StringBuilder Replace char that StringBuilder includes ");

        string oldString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(oldString);
        int  startIndex = TestLibrary.Generator.GetInt32(-55) % (oldString.Length-1);
        char oldChar = oldString[TestLibrary.Generator.GetInt32(-55) % oldString.Length];
        char newChar = TestLibrary.Generator.GetChar(-55);

        while (0 == oldChar.CompareTo(newChar))
        {
            newChar = TestLibrary.Generator.GetChar(-55);
        }


        string replaceSrc = oldString.Replace(oldChar, newChar);

        try
        {
            
            stringBuilder.Replace(oldChar, newChar);
            if (0 != string.CompareOrdinal(stringBuilder.ToString(), replaceSrc))
            {
                TestLibrary.TestFramework.LogError("003", "StringBuilder\"" + oldString + "\" can't corrently Replace \"" + oldChar.ToString() + "\" to \"" + newChar + "\" ");
                retVal = false;
            }
        }
        catch(Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Verify StringBuilder of Empty Replace char of random ");

        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(string.Empty);
        char oldChar = TestLibrary.Generator.GetChar(-55); 
        char newChar = TestLibrary.Generator.GetChar(-55);

        while (0 == oldChar.CompareTo(newChar))
        {
            newChar = TestLibrary.Generator.GetChar(-55);
        }

        try
        {
            stringBuilder.Replace(oldChar, newChar);
            if (stringBuilder.ToString() != string.Empty)
            {
                TestLibrary.TestFramework.LogError("005", "StringBuilder of empty can't corrently Replace \"" + oldChar.ToString() + "\" to \"" + newChar + "\" ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;

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

