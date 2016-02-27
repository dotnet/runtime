// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
/// <summary>
/// StringBuilder.Append(char[],Int32,Int32)
/// </summary>
public class StringBuilderAppend18
{
    public static int Main()
    {
        StringBuilderAppend18 sbAppend18 = new StringBuilderAppend18();
        TestLibrary.TestFramework.BeginTestCase("StringBuilderAppend18");
        if (sbAppend18.RunTests())
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke Append method in the initial StringBuilder 1");
        try
        {
            StringBuilder sb = new StringBuilder();
            char charVal1 = TestLibrary.Generator.GetChar(-55);
            char charVal2 = TestLibrary.Generator.GetChar(-55);
            char charVal3 = TestLibrary.Generator.GetChar(-55);
            char[] charVals = new char[] { charVal1, charVal2, charVal3 };
            int startIndex = 0;
            int charCount = 0;
            sb = sb.Append(charVals, startIndex,charCount);
            if (sb.ToString() != string.Empty)
            {
                TestLibrary.TestFramework.LogError("001", "The ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Invoke Append method in the initial StringBuilder 2");
        try
        {
            StringBuilder sb = new StringBuilder();
            char charVal1 = TestLibrary.Generator.GetChar(-55);
            char charVal2 = TestLibrary.Generator.GetChar(-55);
            char charVal3 = TestLibrary.Generator.GetChar(-55);
            char[] charVals = new char[] { charVal1, charVal2, charVal3 };
            string strVal = new string(charVals);
            int startIndex = 0;
            int charCount = 3;
            sb = sb.Append(charVals, startIndex, charCount);
            if (sb.ToString() != strVal)
            {
                TestLibrary.TestFramework.LogError("003", "The ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Invoke Append method in the initial StringBuilder 3");
        try
        {
            string strSource = "formytest";
            StringBuilder sb = new StringBuilder(strSource);
            char charVal1 = TestLibrary.Generator.GetChar(-55);
            char charVal2 = TestLibrary.Generator.GetChar(-55);
            char charVal3 = TestLibrary.Generator.GetChar(-55);
            char[] charVals = new char[] { charVal1, charVal2, charVal3 };
            string strVal = new string(charVals);
            int startIndex = 0;
            int charCount = 3;
            sb = sb.Append(charVals, startIndex, charCount);
            if (sb.ToString() != strSource + strVal)
            {
                TestLibrary.TestFramework.LogError("005", "The ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4:Invoke Append method in the initial StringBuilder 4");
        try
        {
            string strSource = null;
            StringBuilder sb = new StringBuilder(strSource);
            char charVal1 = TestLibrary.Generator.GetChar(-55);
            char charVal2 = TestLibrary.Generator.GetChar(-55);
            char charVal3 = TestLibrary.Generator.GetChar(-55);
            char[] charVals = new char[] { charVal1, charVal2, charVal3 };
            int startIndex = 1;
            int charCount = 2;
            sb = sb.Append(charVals, startIndex, charCount);
            string strVal = new string(new char[] { charVal2, charVal3 });
            if (sb.ToString() != strVal)
            {
                TestLibrary.TestFramework.LogError("007", "The ExpectResult is not the ActualResult");
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
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1:The char array is null");
        try
        {
            StringBuilder sb = new StringBuilder();
            char[] charVals = null;
            int startIndex = 1;
            int charCount = 1;
            sb = sb.Append(charVals, startIndex, charCount);
            TestLibrary.TestFramework.LogError("N001", "The char array is null but not throw exception");
            retVal = false;
        }
        catch (ArgumentNullException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2:The charCount is less than zero");
        try
        {
            StringBuilder sb = new StringBuilder();
            char[] charVals = new char[] { 'a', 'b', 'c' };
            int startIndex = 0;
            int charCount = this.GetInt32(1, Int32.MaxValue) * (-1);
            sb = sb.Append(charVals, startIndex, charCount);
            TestLibrary.TestFramework.LogError("N003", "The charCount is less than zero but not throw exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest3:The startIndex is less than zero");
        try
        {
            StringBuilder sb = new StringBuilder();
            char[] charVals = new char[] { 'a', 'b', 'c' };
            int startIndex = this.GetInt32(1, 10) * (-1);
            int charCount = charVals.Length;
            sb = sb.Append(charVals, startIndex, charCount);
            TestLibrary.TestFramework.LogError("N005", "The startIndex is less than zero but not throw exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest4:The startIndex plus charCount is larger than char array length");
        try
        {
            StringBuilder sb = new StringBuilder();
            char[] charVals = new char[] { 'a', 'b', 'c' };
            int startIndex = 0;
            int charCount = charVals.Length + 1;
            sb = sb.Append(charVals, startIndex, charCount);
            TestLibrary.TestFramework.LogError("N007", "The startIndex plus charCount is larger than char array length but not throw exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region HelpMethod
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
    #endregion
}
