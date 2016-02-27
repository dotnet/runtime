// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
/// <summary>
/// StringBuilderAppend(String,Int32,Int32)
/// </summary>
public class StringBuilderAppend19
{
    private const int c_MIN_STR_LENGTH = 8;
    private const int c_MAX_STR_LENGTH = 256;
    public static int Main()
    {
        StringBuilderAppend19 sbAppend19 = new StringBuilderAppend19();
        TestLibrary.TestFramework.BeginTestCase("StringBuilderAppend19");
        if (sbAppend19.RunTests())
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
            string strVal = null;
            int startIndex = 0;
            int count = 0;
            sb = sb.Append(strVal, startIndex, count);
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
            string strVal = TestLibrary.Generator.GetString(-55, false,c_MIN_STR_LENGTH,c_MAX_STR_LENGTH);
            int startIndex = 0;
            int count = strVal.Length;
            sb = sb.Append(strVal, startIndex, count);
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
            string strVal = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LENGTH, c_MAX_STR_LENGTH);
            int startIndex = 0;
            int count = strVal.Length;
            sb = sb.Append(strVal, startIndex, count);
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
            string strVal = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LENGTH, c_MAX_STR_LENGTH);
            int startIndex = this.GetInt32(0, strVal.Length);
            int count = strVal.Length - startIndex;
            sb = sb.Append(strVal, startIndex, count);
            if (sb.ToString() != strVal.Substring(startIndex,count))
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
        TestLibrary.TestFramework.BeginScenario("NegTest1:The value of the string is null");
        try
        {
            StringBuilder sb = new StringBuilder();
            string strVal = null;
            int startIndex = 1;
            int count = 1;
            sb = sb.Append(strVal, startIndex, count);
            TestLibrary.TestFramework.LogError("N001", "The value of the string is null but not throw exception");
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
        TestLibrary.TestFramework.BeginScenario("NegTest2:The count is less than zero");
        try
        {
            StringBuilder sb = new StringBuilder();
            string strVal = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LENGTH,c_MAX_STR_LENGTH);
            int startIndex = 0;
            int count = this.GetInt32(1, Int32.MaxValue) * (-1);
            sb = sb.Append(strVal, startIndex, count);
            TestLibrary.TestFramework.LogError("N003", "The count is less than zero but not throw exception");
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
            string strVal = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LENGTH, c_MAX_STR_LENGTH);
            int startIndex = this.GetInt32(1, 10) * (-1);
            int count = strVal.Length;
            sb = sb.Append(strVal, startIndex, count);
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
            string strVal = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LENGTH, c_MAX_STR_LENGTH);
            int startIndex = 0;
            int count = strVal.Length + 1;
            sb = sb.Append(strVal, startIndex, count);
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
