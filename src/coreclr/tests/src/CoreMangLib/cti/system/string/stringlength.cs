// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// String.Length
/// </summary>
public class StringLength
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;

    public static int Main()
    {
        StringLength sl = new StringLength();
        TestLibrary.TestFramework.BeginTestCase("StringLength");

        if (sl.RunTests())
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
    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        string strA;
        int ActualResult;
        TestLibrary.TestFramework.BeginScenario("PosTest1: empty string length");

        try
        {
            strA = string.Empty;
            ActualResult = strA.Length;
            if (ActualResult != 0)
            {
                TestLibrary.TestFramework.LogError("001", "empty string length ActualResult is not the ExpectResult");
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
        int ActualResult;
        TestLibrary.TestFramework.BeginScenario("PosTest2: string with special symbols length one");

        try
        {
            strA = "hello\u0061\u030aworld";
            ActualResult = strA.Length;
            if (ActualResult != 12)
            {
                TestLibrary.TestFramework.LogError("003", "string with special symbols length one ActualResult is not the ExpectResult");
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
        int ActualResult;
        TestLibrary.TestFramework.BeginScenario("PosTest3: string with special symbols length two");

        try
        {
            strA = "This\0String\0Is\0Valid";
            ActualResult = strA.Length;
            if (ActualResult != 20)
            {
                TestLibrary.TestFramework.LogError("005", "string with special symbols length two ActualResult is not the ExpectResult");
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
        int ActualResult;
        TestLibrary.TestFramework.BeginScenario("PosTest4: normal string length");
        try
        {
            strA = new string(this.GetChar(c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH), c_MAX_STRING_LENGTH);
            ActualResult = strA.Length;
            if (ActualResult != c_MAX_STRING_LENGTH)
            {
                TestLibrary.TestFramework.LogError("007", "normal string length ActualResult is not the ExpectResult");
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

