// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Convert.ToChar(UInt32)
/// Converts the value of the specified 32-bit unsigned integer to its equivalent Unicode character. 
/// </summary>
public class ConvertTochar
{
    public static int Main()
    {
        ConvertTochar testObj = new ConvertTochar();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToChar(UInt32)");
        if(testObj.RunTests())
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

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string errorDesc;

        UInt32 i;
        char expectedValue;
        char actualValue;

        i = (UInt32)(TestLibrary.Generator.GetInt64(-55) % (UInt16.MaxValue + 1));

        TestLibrary.TestFramework.BeginScenario("PosTest1: UInt32 value between 0 and UInt16.MaxValue.");
        try
        {
            actualValue = Convert.ToChar(i);
            expectedValue = (char)i;
            if (actualValue != expectedValue)
            {
                errorDesc = string.Format(
                    "The character corresponding to UInt32 value {0} is not \\u{1:x} as expected: actual(\\u{2:x})", 
                    i, (int)expectedValue, (int)actualValue);
                TestLibrary.TestFramework.LogError("001", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe UInt32 value is " + i;
            TestLibrary.TestFramework.LogError("002", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string errorDesc;

        UInt32 i;
        char expectedValue;
        char actualValue;

        i = UInt16.MaxValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2: UInt32 value is UInt16.MaxValue.");
        try
        {
            actualValue = Convert.ToChar(i);
            expectedValue = (char)i;
            if (actualValue != expectedValue)
            {
                errorDesc = string.Format(
                    "The character corresponding to UInt32 value {0} is not \\u{1:x} as expected: actual(\\u{2:x})",
                    i, (int)expectedValue, (int)actualValue);
                TestLibrary.TestFramework.LogError("003", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe UInt32 value is " + i;
            TestLibrary.TestFramework.LogError("004", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string errorDesc;

        UInt32 i;
        char expectedValue;
        char actualValue;

        i = UInt32.MinValue;

        TestLibrary.TestFramework.BeginScenario("PosTest3: value is UInt32.MinValue.");
        try
        {
            actualValue = Convert.ToChar(i);
            expectedValue = (char)i;
            if (actualValue != expectedValue)
            {
                errorDesc = string.Format(
                    "The character corresponding to UInt32 value {0} is not \\u{1:x} as expected: actual(\\u{2:x})",
                    i, (int)expectedValue, (int)actualValue);
                TestLibrary.TestFramework.LogError("005", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe UInt32 value is " + i;
            TestLibrary.TestFramework.LogError("006", errorDesc);
            retVal = false;
        }
        return retVal;
    }
    #endregion

    #region Negative tests
    //OverflowException
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "N001";
        const string c_TEST_DESC = "NegTest1: UInt32 value is a integer between UInt16.MaxValue and UInt32.MaxValue.";
        string errorDesc;

        UInt32 i = (UInt32)(TestLibrary.Generator.GetInt64(-55) % (UInt32.MaxValue - UInt16.MaxValue) + 
                      UInt16.MaxValue + 1);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Convert.ToChar(i);

            errorDesc = "OverflowException is not thrown as expected.";
            errorDesc += string.Format("\nThe UInt32 value is {0}", i);
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nThe UInt32 value is {0}", i);
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
