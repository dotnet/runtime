// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// UInt16.Parse(string)
/// </summary>
public class UInt16Parse
{
    public static int Main()
    {
        UInt16Parse ui32ct2 = new UInt16Parse();
        TestLibrary.TestFramework.BeginTestCase("for method: UInt16.Parse(string)");

        if (ui32ct2.RunTests())
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
        string errorDesc;
        UInt16 actualValue;
        TestLibrary.TestFramework.BeginScenario("PosTest1: UInt16.MaxValue.");
        try
        {
            string strValue = UInt16.MaxValue.ToString();
            actualValue = UInt16.Parse(strValue);
            if (actualValue != UInt16.MaxValue)
            {
                errorDesc = "The parse value of " + strValue + " is not the value " + UInt16.MaxValue +
                    " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("001", errorDesc);
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
        string errorDesc;
        UInt16 actualValue;
        TestLibrary.TestFramework.BeginScenario("PosTest2: UInt16.MinValue.");
        try
        {
            string strValue = UInt16.MinValue.ToString();
            actualValue = UInt16.Parse(strValue);
            if (actualValue != UInt16.MinValue)
            {
                errorDesc = "The parse value of " + strValue + " is not the value " + UInt16.MinValue +
                    " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("003", errorDesc);
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
        string errorDesc;

        UInt16 expectedValue;
        UInt16 actualValue;
        TestLibrary.TestFramework.BeginScenario("PosTest3: random UInt16 value between 0 and UInt16.MaxValue");
        try
        {
            expectedValue = (UInt16)this.GetInt32(0, UInt16.MaxValue);
            string strValue = expectedValue.ToString();
            actualValue = UInt16.Parse(strValue);
            if (actualValue != expectedValue)
            {
                errorDesc = "The parse value of " + strValue + " is not the value " + expectedValue +
                    " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("005", "the ActualResult is not the ExpectResult");
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
    #endregion

    #region Negative tests
    //ArgumentNullException
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "N001";
        const string c_TEST_DESC = "NegTest1: string representation is a null reference";
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt16.Parse(null);
            errorDesc = "ArgumentNullException is not thrown as expected.";
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (ArgumentNullException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    //FormatException
    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "N002";
        const string c_TEST_DESC = "NegTest2: String representation is not in the correct format";
        string errorDesc;

        string strValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            strValue = "Incorrect";
            UInt16.Parse(strValue);
            errorDesc = "FormatException is not thrown as expected.";
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (FormatException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    //OverflowException
    public bool NegTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "N003";
        const string c_TEST_DESC = "NegTest3: String representation is greater than UInt16.MaxValue";
        string errorDesc;

        string strValue;
        int i;

        i = this.GetInt32(UInt16.MaxValue + 1, int.MaxValue);
        strValue = i.ToString();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt16.Parse(strValue);
            errorDesc = "OverflowException is not thrown as expected.";
            errorDesc += "\nThe string representation is " + strValue;
            TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += "\nThe string representation is " + strValue;
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        const string c_TEST_ID = "N004";
        const string c_TEST_DESC = "NegTest4: String representation is less than UInt16.MaxValue";
        string errorDesc;

        string strValue;
        int i;

        i = -1 * TestLibrary.Generator.GetInt32(-55) - 1;
        strValue = i.ToString();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt16.Parse(strValue);
            errorDesc = "OverflowException is not thrown as expected.";
            errorDesc += "\nThe string representation is " + strValue;
            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += "\nThe string representation is " + strValue;
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }
    #endregion

    #region ForTestObject
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
