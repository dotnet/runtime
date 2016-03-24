// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// Char.System.IConvertible.ToUInt32(IFormatProvider)
/// Converts the value of the current Char object to an 32-bit unsigned integer. 
/// </summary>
public class CharIConvertibleToUInt32
{
    public static int Main()
    {
        CharIConvertibleToUInt32 testObj = new CharIConvertibleToUInt32();

        TestLibrary.TestFramework.BeginTestCase("for method: Char.System.IConvertible.ToUInt32(IFormatProvider)");
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
        const string c_TEST_DESC = "PosTest1: Random character.";
        string errorDesc;

        UInt32 expectedValue;
        UInt32 actualValue;
        char ch;
        expectedValue = (UInt32)(TestLibrary.Generator.GetInt32(-55) % (UInt16.MaxValue + 1));
        ch = (char)expectedValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            NumberFormatInfo numberFormat = new NumberFormatInfo();
            IFormatProvider provider = numberFormat;
            IConvertible converter = ch;

            actualValue = converter.ToUInt32(numberFormat);
            
            if (actualValue != expectedValue)
            {
                errorDesc = string.Format("UInt32 value of character \\u{0:x} is not ", (int)ch);
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

