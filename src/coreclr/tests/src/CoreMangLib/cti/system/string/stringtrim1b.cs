// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// String.Trim()
/// </summary>
public class StringTrim1
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
        StringTrim1 st1 = new StringTrim1();
        TestLibrary.TestFramework.BeginTestCase("StringTrim1");
        //while (st1.RunTests())
        //{
        //    Console.WriteLine(")");
        //}
        //return 100;
        if (st1.RunTests())
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

    public bool PosTest1()
    {
        bool retVal = true;
        string strA;
        string ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest1: empty string trim()");

        try
        {
            strA = string.Empty;
            ActualResult = strA.Trim();
            if (ActualResult != string.Empty)
            {
                TestLibrary.TestFramework.LogError("001", "empty string trim() ActualResult is not the ExpectResult");
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
        string ActualResult;
      
        TestLibrary.TestFramework.BeginScenario("PosTest2:normal string trim one");
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
			int i = this.GetInt32(0, spaceStrings.Length);
            string strB = spaceStrings[i];
            string strA1 = strB + "H" + strA + "D" + strB;
            ActualResult = strA1.Trim();
            if (ActualResult.ToString() != "H" + strA.ToString() + "D")
            {
				TestLibrary.TestFramework.LogError("003", "normal string trim one when space is (" + i + ":'" + strB.ToString() + "') ActualResult is not the ExpectResult");
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
        string ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest3:normal string trim two");
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
			int i = this.GetInt32(0, spaceStrings.Length);
			string strB = spaceStrings[i];
            string strA1 = strB + "H" + strB + strA + strB+ "D" + strB;
            ActualResult = strA1.Trim();
            if (ActualResult.ToString() != "H" + strB + strA.ToString() + strB + "D")
            {
				TestLibrary.TestFramework.LogError("005", "normal string trim one when space is ("+i+":'" + strB.ToString() + "') ActualResult is not the ExpectResult");
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

