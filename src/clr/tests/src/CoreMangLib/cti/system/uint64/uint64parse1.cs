// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

/// <summary>
/// UInt64.Parse(System.string)
/// </summary>
public class UInt64Parse1
{
    public static int Main()
    {
        UInt64Parse1 ui64parse1 = new UInt64Parse1();
        TestLibrary.TestFramework.BeginTestCase("UInt64Parse1");

        if (ui64parse1.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;

        return retVal;
    }
    #region PostiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        UInt64 uintA;

        TestLibrary.TestFramework.BeginScenario("PosTest1: the string corresponding UInt64 is UInt64 MinValue ");

        try
        {
            string strA = UInt64.MinValue.ToString();
            uintA = UInt64.Parse(strA);
            if (uintA != 0)
            {
                TestLibrary.TestFramework.LogError("001", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        UInt64 uintA;

        TestLibrary.TestFramework.BeginScenario("PosTest2: the string corresponding UInt64 is UInt64 MaxValue ");

        try
        {
            string strA = UInt64.MaxValue.ToString();
            uintA = UInt64.Parse(strA);
            if (uintA != UInt64.MaxValue)
            {
                TestLibrary.TestFramework.LogError("003", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest3()
    {
        bool retVal = true;
        UInt64 uintA;

        TestLibrary.TestFramework.BeginScenario("PosTest3: the string corresponding UInt64 is normal UInt64 ");

        try
        {
            UInt64 uintTest = (UInt64)this.GetInt64(0, UInt64.MaxValue);
            string strA = uintTest.ToString();
            uintA = UInt64.Parse(strA);
            if (uintA != uintTest)
            {
                TestLibrary.TestFramework.LogError("005", "the ActualResult is not the ExpectResult,The UInt64 is:" + uintTest);
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
    public bool PosTest4()
    {
        bool retVal = true;
        UInt64 uintA;

        TestLibrary.TestFramework.BeginScenario("PosTest4: the string format is [ws][sign]digits[ws] 1");

        try
        {
            UInt64 uintTest = (UInt64)this.GetInt64(0, UInt64.MaxValue);
            string strA = "+" + uintTest.ToString();
            uintA = UInt64.Parse(strA);
            if (uintA != uintTest)
            {
                TestLibrary.TestFramework.LogError("007", "the ActualResult is not the ExpectResult,UInt64 is:" + uintTest);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest5()
    {
        bool retVal = true;
        UInt64 uintA;

        TestLibrary.TestFramework.BeginScenario("PosTest5: the string format is [ws][sign]digits[ws] 2");

        try
        {
            UInt64 uintTest = (UInt64)this.GetInt64(0, UInt64.MaxValue);
            string strA = "\u0020" + uintTest.ToString();
            uintA = UInt64.Parse(strA);
            if (uintA != uintTest)
            {
                TestLibrary.TestFramework.LogError("009", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest6()
    {
        bool retVal = true;
        UInt64 uintA;

        TestLibrary.TestFramework.BeginScenario("PosTest6: the string format is [ws][sign]digits[ws] 3");

        try
        {
            UInt64 uintTest = (UInt64)this.GetInt64(0, UInt64.MaxValue);
            string strA = uintTest.ToString() + "\u0020";
            uintA = UInt64.Parse(strA);
            if (uintA != uintTest)
            {
                TestLibrary.TestFramework.LogError("011", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest7()
    {
        bool retVal = true;
        UInt64 uintA;

        TestLibrary.TestFramework.BeginScenario("PosTest7: the string format is [ws][sign]digits[ws] 4");

        try
        {
            UInt64 uintTest = (UInt64)this.GetInt64(0, UInt64.MaxValue);
            string strA = "\u0009" + "+" + uintTest.ToString() + "\u0020";
            uintA = UInt64.Parse(strA);
            if (uintA != uintTest)
            {
                TestLibrary.TestFramework.LogError("013", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        UInt64 uintA;
        TestLibrary.TestFramework.BeginScenario("NegTest1: the parameter string is null");

        try
        {
            string strA = null;
            uintA = UInt64.Parse(strA);
            retVal = false;
        }
        catch (ArgumentNullException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N001", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest2()
    {
        bool retVal = true;
        UInt64 uintA;
        TestLibrary.TestFramework.BeginScenario("NegTest2: the parameter string is not of the correct format 1");

        try
        {
            string strA = "abcd";
            uintA = UInt64.Parse(strA);
            retVal = false;
        }
        catch (FormatException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest3()
    {
        bool retVal = true;
        UInt64 uintA;
        TestLibrary.TestFramework.BeginScenario("NegTest3: the parameter string is not of the correct format 2");

        try
        {
            string strA = "b12345d";
            uintA = UInt64.Parse(strA);
            retVal = false;
        }
        catch (FormatException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N003", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest4()
    {
        bool retVal = true;
        UInt64 uintA;
        TestLibrary.TestFramework.BeginScenario("NegTest4: the parameter string corresponding number is less than UInt64 minValue");

        try
        {
            Int64 Testint = (-1) * Convert.ToInt64(this.GetInt64(1, Int64.MaxValue));
            string strA = Testint.ToString();
            uintA = UInt64.Parse(strA);
            retVal = false;
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N004", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest5()
    {
        bool retVal = true;
        UInt64 uintA;
        TestLibrary.TestFramework.BeginScenario("NegTest5: the parameter string corresponding number is larger than UInt64 maxValue");

        try
        {
            string strA = "18446744073709551616";
            uintA = UInt64.Parse(strA);
            retVal = false;
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N005", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region ForTestObject
    private UInt64 GetInt64(UInt64 minValue, UInt64 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return minValue + (UInt64)TestLibrary.Generator.GetInt64(-55) % (maxValue - minValue);
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

