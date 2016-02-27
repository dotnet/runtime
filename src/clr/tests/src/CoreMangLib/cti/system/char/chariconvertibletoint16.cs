// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization; //for number format info

/// <summary>
/// Char.System.IConvertible.ToInt16(IFormatProvider)
/// Converts the value of the current Char object to an 16-bit signed integer. 
/// </summary>
public class CharIConvertibleToInt16
{
    public static int Main()
    {
        CharIConvertibleToInt16 testObj = new CharIConvertibleToInt16();

        TestLibrary.TestFramework.BeginTestCase("for method: Char.System.IConvertible.ToInt16(IFormatProvider)");
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

        TestLibrary.TestFramework.LogInformation("");
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: Random valid character from 0 to Int16.MaxValue";
        string errorDesc;

        Int16 expectedValue;
        Int16 actualValue;
        char ch;
        expectedValue = TestLibrary.Generator.GetInt16(-55);
        ch = (char)expectedValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            NumberFormatInfo numberFormat = new NumberFormatInfo();
            IFormatProvider provider = numberFormat;
            IConvertible converter = ch;

            actualValue = converter.ToInt16(numberFormat);
            
            if (actualValue != expectedValue)
            {
                errorDesc = string.Format("Int16 value of character \\u{0:x} is not ", (int)ch);
                errorDesc += expectedValue + " as expected: Actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    //bug
    //overflow exception
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        const string c_TEST_DESC = "NegTest1: Random valid character from Int16.MaxValue + 1 to UInt16.MaxValue";

        UInt16 ui;
        Int16 expectedValue;
        Int16 actualValue;
        char ch;
        ui = (UInt16)(Int16.MaxValue +  1 + 
                           TestLibrary.Generator.GetInt32(-55) % (UInt16.MaxValue - Int16.MaxValue));
        ch = (char)ui;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            NumberFormatInfo numberFormat = new NumberFormatInfo();
            IFormatProvider provider = numberFormat;
            IConvertible converter = ch;

            expectedValue = (Int16)ui;
            actualValue = converter.ToInt16(numberFormat);

            TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, c_TEST_DESC);
            TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, "Exception expected");
            retVal = false;
        }
        catch (OverflowException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, c_TEST_DESC);
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}

