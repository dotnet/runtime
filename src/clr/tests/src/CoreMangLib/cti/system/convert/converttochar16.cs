// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Convert.ToChar(UInt64)
/// Converts the value of the specified 64-bit unsigned integer to its equivalent Unicode character. 
/// </summary>
public class ConvertTochar
{
    public static int Main()
    {
        ConvertTochar testObj = new ConvertTochar();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToChar(UInt64)");
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

        UInt64 i;
        char expectedValue;
        char actualValue;

        i = GetUInt64() % (UInt16.MaxValue + 1);

        TestLibrary.TestFramework.BeginScenario("PosTest1: UInt64 value between 0 and UInt16.MaxValue.");
        try
        {
            actualValue = Convert.ToChar(i);
            expectedValue = (char)i;
            if (actualValue != expectedValue)
            {
                errorDesc = string.Format(
                    "The character corresponding to UInt64 value {0} is not \\u{1:x} as expected: actual(\\u{2:x})", 
                    i, (int)expectedValue, (int)actualValue);
                TestLibrary.TestFramework.LogError("001", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe UInt64 value is " + i;
            TestLibrary.TestFramework.LogError("002", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string errorDesc;

        UInt64 i;
        char expectedValue;
        char actualValue;

        i = UInt16.MaxValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2: UInt64 value is UInt16.MaxValue.");
        try
        {
            actualValue = Convert.ToChar(i);
            expectedValue = (char)i;
            if (actualValue != expectedValue)
            {
                errorDesc = string.Format(
                    "The character corresponding to UInt64 value {0} is not \\u{1:x} as expected: actual(\\u{2:x})",
                    i, (int)expectedValue, (int)actualValue);
                TestLibrary.TestFramework.LogError("003", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe UInt64 value is " + i;
            TestLibrary.TestFramework.LogError("004", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string errorDesc;

        UInt64 i;
        char expectedValue;
        char actualValue;

        i = UInt64.MinValue;

        TestLibrary.TestFramework.BeginScenario("PosTest3: value is UInt64.MinValue.");
        try
        {
            actualValue = Convert.ToChar(i);
            expectedValue = (char)i;
            if (actualValue != expectedValue)
            {
                errorDesc = string.Format(
                    "The character corresponding to UInt64 value {0} is not \\u{1:x} as expected: actual(\\u{2:x})",
                    i, (int)expectedValue, (int)actualValue);
                TestLibrary.TestFramework.LogError("005", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe UInt64 value is " + i;
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
        const string c_TEST_DESC = "NegTest1: UInt64 value is a integer between UInt16.MaxValue and UInt64.MaxValue.";
        string errorDesc;

        UInt64 i = (UInt64)(GetUInt64() % (UInt64.MaxValue - UInt16.MaxValue) + UInt16.MaxValue + 1);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Convert.ToChar(i);

            errorDesc = "OverflowException is not thrown as expected.";
            errorDesc += string.Format("\nThe UInt64 value is {0}", i);
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nThe UInt64 value is {0}", i);
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region helper method
    // returns a non-negative UInt64 between 0 and UInt64.MaxValue
    private static UInt64 GetUInt64()
    {
        byte[] buffer = new byte[8];
        UInt64 iVal;

        TestLibrary.Generator.GetBytes(-55, buffer);

        // convert to Int64
        iVal = 0;
        for (int i = 0; i < buffer.Length; i++)
        {
            iVal |= ((UInt64)buffer[i] << (i * 8));
        }

        TestLibrary.TestFramework.LogInformation("Random Int64 produced: " + iVal.ToString());
        return iVal;
    }
    #endregion
}
