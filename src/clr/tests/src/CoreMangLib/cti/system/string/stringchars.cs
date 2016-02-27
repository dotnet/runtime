// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// String.chars
/// </summary>
public class StringChars
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;
    public static int Main()
    {
        StringChars sc = new StringChars();
        TestLibrary.TestFramework.BeginTestCase("StringChars");

        if (sc.RunTests())
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
        retVal = NegTest3() && retVal;
        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        string strA;
        char ActualResult1;
        char ActualResult2;
        TestLibrary.TestFramework.BeginScenario("PosTest1: string's chars one");
        try
        {
            strA = "Hello\u0009 World";
            ActualResult1 = strA[5];
            ActualResult2 = strA[6];
            if (ActualResult1 != '\t' || ActualResult2 !='\u0020')
            {
                TestLibrary.TestFramework.LogError("001", "string's chars one ActualResult is not the ExpectResult");
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        string strA;
        char ActualResult;
        TestLibrary.TestFramework.BeginScenario("PosTest2: string's chars two");
        try
        {
            strA = this.GetString(false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            char charA = this.GetChar(0, c_MINI_STRING_LENGTH);
            int Insert = this.GetInt32(0, strA.Length + 1);
            string strA1 = strA.Insert(Insert, charA.ToString());
            ActualResult = strA1[Insert];
            if (ActualResult!= charA)
            {
                TestLibrary.TestFramework.LogError("003", "string's chars two ActualResult is not the ExpectResult");
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region NegativeTesting
    public bool NegTest1()
    {
        bool retVal = true;
        string strA;
        char ActualResult;
        TestLibrary.TestFramework.BeginScenario("NegTest1: index is equel string's length");
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            ActualResult = strA[strA.Length];
            retVal = false;
        }
        catch (IndexOutOfRangeException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N001", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest2()
    {
        bool retVal = true;
        string strA;
        char ActualResult;
        TestLibrary.TestFramework.BeginScenario("NegTest2: index is greater than string's length");
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            ActualResult = strA[this.GetInt32(strA.Length + 1,Int32.MaxValue)];
            retVal = false;
        }
        catch (IndexOutOfRangeException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest3()
    {
        bool retVal = true;
        string strA;
        char ActualResult;
        TestLibrary.TestFramework.BeginScenario("NegTest3: index is less than 0");
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            ActualResult = strA[this.GetInt32(1,strA.Length)*(-1)];
            retVal = false;
        }
        catch (IndexOutOfRangeException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N003", "Unexpected exception:" + e);
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

