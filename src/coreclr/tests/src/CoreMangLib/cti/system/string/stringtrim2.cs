// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// String.Trim(char[])
/// </summary>
public class StringTrim2
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;

	// U+200B stops being trimmable http://msdn2.microsoft.com/en-us/library/t97s7bs3.aspx
	// U+FEFF has been deprecate as a trimmable space
	private string[] spaceStrings = new string[]{"\u0009","\u000A","\u000B","\u000C","\u000D","\u0020",
                               "\u00A0","\u2000","\u2001","\u2002","\u2003","\u2004","\u2005",
                                "\u2006","\u2007","\u2008","\u2009","\u200A","\u3000"};


    public static int Main()
    {
        StringTrim2 st2 = new StringTrim2();
        TestLibrary.TestFramework.BeginTestCase("StringTrim2");

        if (st2.RunTests())
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
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;
        return retVal;
    }
    #region PostiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        string strA;
        char[] charA;
        string ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest1: empty string trim char[]");

        try
        {
            strA = string.Empty;
            charA = new char[] {TestLibrary.Generator.GetChar(-55),TestLibrary.Generator.GetChar(-55),TestLibrary.Generator.GetChar(-55) };
            ActualResult = strA.Trim(charA);
            if (ActualResult != string.Empty)
            {
                TestLibrary.TestFramework.LogError("001", "empty string trim char[] ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        string strA;
        char[] charA;
        string ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest2:normal string trim char[] one");
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            char char1 = this.GetChar(0, c_MINI_STRING_LENGTH);
            char char2 = this.GetChar(c_MINI_STRING_LENGTH, c_MINI_STRING_LENGTH + 68);
            char char3 = this.GetChar(c_MINI_STRING_LENGTH + 68, c_MAX_STRING_LENGTH / 2);
            char charEnd = this.GetChar(c_MAX_STRING_LENGTH / 2, c_MAX_STRING_LENGTH);
            charA = new char[] { char1, char2, char3 };

            string strA1 = char1.ToString() + char3.ToString() + charEnd.ToString() + strA + charEnd.ToString();
            ActualResult = strA1.Trim(charA);
            if (ActualResult.ToString() !=  charEnd.ToString() + strA.ToString() + charEnd.ToString())
            {
                TestLibrary.TestFramework.LogError("003", "normal string trim char[] one ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest3()
    {
        bool retVal = true;
        string strA;
        char[] charA;
        string ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest3:normal string trim char[] two");
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            char char1 = this.GetChar(0, c_MINI_STRING_LENGTH);
            char char2 = this.GetChar(c_MINI_STRING_LENGTH, c_MINI_STRING_LENGTH + 68);
            char char3 = this.GetChar(c_MAX_STRING_LENGTH + 68, c_MAX_STRING_LENGTH / 2);
            char charStart = this.GetChar(c_MAX_STRING_LENGTH / 2, c_MAX_STRING_LENGTH);
            charA = new char[] { char1, char2, char3 };

            string strA1 = charStart.ToString() + strA + charStart.ToString() + char2.ToString() + char3.ToString();
            ActualResult = strA1.Trim(charA);
            if (ActualResult.ToString() != charStart.ToString() + strA + charStart.ToString())
            {
                TestLibrary.TestFramework.LogError("005", "normal string trim char[] two ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest4()
    {
        bool retVal = true;
        string strA;
        char[] charA;
        string ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest4:normal string trim char[] three");
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            char char1 = this.GetChar(0, c_MINI_STRING_LENGTH);
            char char2 = this.GetChar(c_MINI_STRING_LENGTH, c_MINI_STRING_LENGTH + 68);
            char char3 = this.GetChar(c_MAX_STRING_LENGTH + 68, c_MAX_STRING_LENGTH / 2);
            char charStart = this.GetChar(c_MAX_STRING_LENGTH / 2, c_MAX_STRING_LENGTH);
            charA = new char[] { char1, char2, char3 };

            string strA1 = char1.ToString() + charStart.ToString() + char2.ToString() + strA + charStart.ToString() + char3.ToString();
            ActualResult = strA1.Trim(charA);
            if (ActualResult.ToString() != charStart.ToString() + char2.ToString() + strA + charStart.ToString())
            {
                TestLibrary.TestFramework.LogError("007", "normal string trim char[] three ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest5()
    {
        bool retVal = true;
        string strA;
        char[] charA;
        string ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest5:normal string trim char[] four");
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            charA = new char[0];
            string strB = spaceStrings[this.GetInt32(0,spaceStrings.Length)];
            string strA1 = strB + "H" + strA + "D";
            ActualResult = strA1.Trim(charA);
            if (ActualResult.ToString() != "H" + strA + "D")
            {
                TestLibrary.TestFramework.LogError("009", "normal string trim char[] four ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest6()
    {
        bool retVal = true;
        string strA;
        char[] charA;
        string ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest6:normal string trim char[] five");
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            charA = new char[0];
            string strB = spaceStrings[this.GetInt32(0, spaceStrings.Length)];
            string strA1 = strB + "H" + strB + strA + "D" + strB;
            ActualResult = strA1.Trim(charA);
            if (ActualResult.ToString() != "H" + strB + strA + "D")
            {
                TestLibrary.TestFramework.LogError("011", "normal string trim char[] five ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest7()
    {
        bool retVal = true;
        string strA;
        char[] charA;
        string ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest7:normal string trim char[] six");
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            charA = new char[0];
            string strB = spaceStrings[this.GetInt32(0, spaceStrings.Length)];
            string strA1 = "H" + strA + "D" + strB;
            ActualResult = strA1.Trim(charA);
            if (ActualResult.ToString() != "H" + strA + "D")
            {
                TestLibrary.TestFramework.LogError("013", "normal string trim char[] six ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion

    #region Help method for geting test data
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
    private Char GetChar(Int32 minValue, Int32 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return Convert.ToChar(minValue);
            }
            if (minValue < maxValue)
            {
                return Convert.ToChar(Convert.ToInt32(TestLibrary.Generator.GetChar(-55)) % (maxValue - minValue) + minValue);
            }
        }
        catch
        {
            throw;
        }
        return Convert.ToChar(minValue);
    }
    private string GetString(bool ValidPath, Int32 minValue, Int32 maxValue)
    {
        StringBuilder sVal = new StringBuilder();
        string s;

        if (0 == minValue && 0 == maxValue) return String.Empty;
        if (minValue > maxValue) return null;

        if (ValidPath)
        {
            return TestLibrary.Generator.GetString(-55, ValidPath, minValue, maxValue);
        }
        else
        {
            int length = this.GetInt32(minValue, maxValue);
            for (int i = 0; length > i; i++)
            {
                char c = this.GetChar(minValue, maxValue);
                sVal.Append(c);
            }
            s = sVal.ToString();
            return s;
        }


    }
    #endregion
}

