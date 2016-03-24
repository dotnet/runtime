// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// Char.System.IConvertible.ToSByte(IFormatProvider)
/// Converts the value of the current Char object to an 8-bit signed integer. 
/// </summary>
public class CharIConvertibleToSByte
{
    public static int Main()
    {
        CharIConvertibleToSByte testObj = new CharIConvertibleToSByte();

        TestLibrary.TestFramework.BeginTestCase("for method: Char.System.IConvertible.ToSByte(IFormatProvider)");
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: Random valid character from 0 to SByte.MaxValue";
        string errorDesc;

        SByte expectedValue;
        SByte actualValue;
        char ch;
        expectedValue = (sbyte)(TestLibrary.Generator.GetByte(-55) % (sbyte.MaxValue + 1));
        ch = (char)expectedValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            NumberFormatInfo numberFormat = new NumberFormatInfo();
            IFormatProvider provider = numberFormat;
            IConvertible converter = ch;

            actualValue = converter.ToSByte(numberFormat);
            
            if (actualValue != expectedValue)
            {
                errorDesc = string.Format("SByte value of character \\u{0:x} is not ", (int)ch);
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

    #region Negative tests
    //bug
    //OverflowException
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "N001";
        const string c_TEST_DESC = "NegTest1: Character with integer value greater than SByte.MaxValue";
        string errorDesc;

        UInt16 iValue;
        char ch;
        // we need a UInt16 between (SByte.MaxValue + 1) and (UInt16.MaxValue)
        int rangeSize = UInt16.MaxValue - (SByte.MaxValue + 1);
        int rawRandom = TestLibrary.Generator.GetInt32(-55);
        iValue = (UInt16)((rawRandom % rangeSize) +
            (SByte.MaxValue + 1));

        ch = (char)iValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            IConvertible converter = ch;
            converter.ToSByte(null);

            errorDesc = "OverflowException is not thrown as expected.";
            errorDesc +=  string.Format("\nThe character is \\u{0:x}", (int)ch);
            TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nThe character is \\u{0:x}", (int)ch);
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}

