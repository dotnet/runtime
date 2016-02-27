// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
/// <summary>
/// StringBuilder.ctor(String,Int32)
/// </summary>
public class StringBuilderctor5
{
    public static int Main()
    {
        StringBuilderctor5 sbctor5 = new StringBuilderctor5();
        TestLibrary.TestFramework.BeginTestCase("StringBuilderctor5");
        if (sbctor5.RunTests())
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Initialize StringBuilder with capacity and string 1");
        try
        {
            string strValue = null;
            int capacity = this.GetInt32(1, 256);
            StringBuilder sb = new StringBuilder(strValue, capacity);
            if (sb == null)
            {
                TestLibrary.TestFramework.LogError("007.1", "StringBuilder was null");
                retVal = false;
            }
            else if (!sb.ToString().Equals(String.Empty))
            {
                TestLibrary.TestFramework.LogError("007.2", "Expected value of StringBuilder.ToString = String.Empty, actual: " +
                    sb.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Initialize StringBuilder with capacity and string 2");
        try
        {
            string strValue = string.Empty;
            int capacity = this.GetInt32(1, 256);
            StringBuilder sb = new StringBuilder(strValue, capacity);
            if (sb == null)
            {
                TestLibrary.TestFramework.LogError("003.1", "StringBuilder was null");
                retVal = false;
            }
            else if (!sb.ToString().Equals(strValue))
            {
                TestLibrary.TestFramework.LogError("003.2", "Expected value of StringBuilder.ToString = " + strValue + ", actual: " +
                    sb.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Initialize StringBuilder with capacity and string 3");
        try
        {
            string strValue = TestLibrary.Generator.GetString(-55, false, 8, 256);
            int capacity = this.GetInt32(1, strValue.Length);
            StringBuilder sb = new StringBuilder(strValue, capacity);
            if (sb == null)
            {
                TestLibrary.TestFramework.LogError("005.1", "StringBuilder was null");
                retVal = false;
            }
            else if (!sb.ToString().Equals(strValue))
            {
                TestLibrary.TestFramework.LogError("005.2", "Expected value of StringBuilder.ToString = " + strValue + ", actual: " +
                    sb.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest4:Initialize StringBuilder with capacity and string 4");
        try
        {
            string strValue = string.Empty;
            int capacity = 0;
            StringBuilder sb = new StringBuilder(strValue, capacity);
            if (sb == null)
            {
                TestLibrary.TestFramework.LogError("007.1", "StringBuilder was null");
                retVal = false;
            }
            else if (!sb.ToString().Equals(strValue))
            {
                TestLibrary.TestFramework.LogError("007.2", "Expected value of StringBuilder.ToString = " + strValue + ", actual: " +
                    sb.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest5:Initialize StringBuilder with capacity and string 5");
        try
        {
            string strValue = TestLibrary.Generator.GetString(-55, false, 8, 256);
            int capacity = 0;
            StringBuilder sb = new StringBuilder(strValue, capacity);
            if (sb == null) 
                    {
                TestLibrary.TestFramework.LogError("009.0", "StringBuilder was null");
                retVal = false;
            } else if (!sb.ToString().Equals(strValue)) 
            {
                TestLibrary.TestFramework.LogError("009.1", "Initializer string was "+strValue+", StringBuilder.ToString returned "+sb.ToString());
                retVal = false;
            }
            else if (sb.Capacity == 0)
            {
                TestLibrary.TestFramework.LogError("009.2", "StringBuilder.Capacity returned 0 for non-empty string");
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
            int capacity = this.GetInt32(1, Int32.MaxValue) * (-1);
            StringBuilder sb = new StringBuilder(strValue, capacity);
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
