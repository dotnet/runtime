// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
/// <summary>
/// StringBuilder.ctor(String,Int32,Int32,Int32)
/// </summary>
public class StringBuilderctor6
{
    public static int Main()
    {
        StringBuilderctor6 sbctor6 = new StringBuilderctor6();
        TestLibrary.TestFramework.BeginTestCase("StringBuilderctor6");
        if (sbctor6.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Initialize StringBuilder with substring and capacity 1");
        try
        {
            string strValue = null;
            int capacity = this.GetInt32(1, 256);
            StringBuilder sb = new StringBuilder(strValue, 0, 0, capacity);
            if (sb == null || sb.ToString() != string.Empty || sb.Capacity != capacity)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Initialize StringBuilder with substring and capacity 2");
        try
        {
            string strValue = string.Empty;
            int startIndex = 0;
            int length = 0;
            int capacity = this.GetInt32(1, 256);
            StringBuilder sb = new StringBuilder(strValue, startIndex, length, capacity);
            if (sb == null || sb.ToString() != string.Empty || sb.Capacity != capacity)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Initialize StringBuilder with substring and capacity 3");
        try
        {
            string strValue = TestLibrary.Generator.GetString(-55, false, 8, 256);
            int startIndex = 0;
            int length = strValue.Length;
            int capacity = this.GetInt32(1, 256);
            StringBuilder sb = new StringBuilder(strValue, startIndex, length, capacity);
            if (sb == null || sb.ToString() != strValue)
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
        TestLibrary.TestFramework.BeginScenario("PosTest4:Initialize StringBuilder with substring and capacity 4");
        try
        {
            string strValue = TestLibrary.Generator.GetString(-55, false, 8, 256);
            int startIndex = this.GetInt32(0, strValue.Length);
            int length = strValue.Length - startIndex;
            int capacity = this.GetInt32(1, 256);
            StringBuilder sb = new StringBuilder(strValue, startIndex, length, capacity);
            if (sb == null || sb.ToString() != strValue.Substring(startIndex,length))
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
    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5:Initialize StringBuilder with substring and capacity 5");
        try
        {
            string strValue = string.Empty;
            int startIndex = 0;
            int length = strValue.Length;
            int capacity = 0;
            StringBuilder sb = new StringBuilder(strValue, startIndex, length, capacity);
            if (sb == null || sb.ToString() != string.Empty || sb.Capacity != 16)
            {
                TestLibrary.TestFramework.LogError("009", "The ExpectResult is not the ActualResult");
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
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1:The capacity is less than zero");
        try
        {
            string strValue = TestLibrary.Generator.GetString(-55, false, 8, 256);
            int startIndex = 0;
            int length = strValue.Length;
            int capacity = this.GetInt32(1, Int32.MaxValue) * (-1);
            StringBuilder sb = new StringBuilder(strValue,startIndex,length,capacity);
            TestLibrary.TestFramework.LogError("N001", "The capacity is less than zero but not throw exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
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
        TestLibrary.TestFramework.BeginScenario("NegTest2:The startIndex plus length is not a position within value");
        try
        {
            string strValue = TestLibrary.Generator.GetString(-55, false, 8, 256);
            int startIndex = 1;
            int length = strValue.Length;
            int capacity = this.GetInt32(0, 256);
            StringBuilder sb = new StringBuilder(strValue, startIndex, length, capacity);
            TestLibrary.TestFramework.LogError("N003", "The startIndex plus length is not a position within value but not throw exception");
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
