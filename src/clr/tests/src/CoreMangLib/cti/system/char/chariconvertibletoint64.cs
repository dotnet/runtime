// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// Char.System.IConvertible.ToInt64(IFormatProvider)
/// Converts the value of the current Char object to an 64-bit signed integer. 
/// </summary>
public class CharIConvertibleToInt64
{
    public static int Main()
    {
        CharIConvertibleToInt64 testObj = new CharIConvertibleToInt64();

        TestLibrary.TestFramework.BeginTestCase("for method: Char.System.IConvertible.ToInt64(IFormatProvider)");
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

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: Random valid character from 0 to UInt16.MaxValue";
        string errorDesc;

        Int64 expectedValue;
        Int64 actualValue;
        char ch;
        expectedValue = TestLibrary.Generator.GetInt32(-55) % (UInt16.MaxValue +  1);
        ch = (char)expectedValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            NumberFormatInfo numberFormat = new NumberFormatInfo();
            IFormatProvider provider = numberFormat;
            IConvertible converter = ch;

            actualValue = converter.ToInt64(numberFormat);
            
            if (actualValue != expectedValue)
            {
                errorDesc = string.Format("Int64 value of character \\u{0:x} is not ", (int)ch);
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
    #endregion
}

