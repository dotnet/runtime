// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
/// <summary>
/// StringBuilder.Append(Char,Int32)
/// </summary>
public class StringBuilderAppend17
{
    public static int Main()
    {
        StringBuilderAppend17 sbAppend17 = new StringBuilderAppend17();
        TestLibrary.TestFramework.BeginTestCase("StringBuilderAppend17");
        if (sbAppend17.RunTests())
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
            char charValue = TestLibrary.Generator.GetChar(-55);
            int repeatCount = 0;
            sb = sb.Append(charValue, repeatCount);
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
            char charValue = TestLibrary.Generator.GetChar(-55);
            int repeatCount = 1;
            sb = sb.Append(charValue, repeatCount);
            if (sb.ToString() != charValue.ToString())
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
            char charValue = TestLibrary.Generator.GetChar(-55);
            int repeatCout = this.GetInt32(1, 100);
            sb = sb.Append(charValue, repeatCout);
            string strVal = new string(charValue, repeatCout);
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
            char charValue = TestLibrary.Generator.GetChar(-55);
            int repeatCount = this.GetInt32(1, 100);
            sb = sb.Append(charValue, repeatCount);
            string strVal = new string(charValue, repeatCount);
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
        TestLibrary.TestFramework.BeginScenario("NegTest1:The param of repeatCount is less than zero");
        try
        {
            StringBuilder sb = new StringBuilder();
            char charValue = TestLibrary.Generator.GetChar(-55);
            int repeatCount = this.GetInt32(1, 100) * (-1);
            sb = sb.Append(charValue, repeatCount);
            TestLibrary.TestFramework.LogError("N001", "The param of repeatCount is less than zero but not throw exception");
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
