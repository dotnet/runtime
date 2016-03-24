// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// Char.System.IConvertible.ToChar(IFormatProvider)
/// Return the current char object.
/// </summary>
public class CharIConvertibleToChar
{
    public static int Main()
    {
        CharIConvertibleToChar testObj = new CharIConvertibleToChar();

        TestLibrary.TestFramework.BeginTestCase("for method: Char.System.IConvertible.Tochar(IFormatProvider)");
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

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: Random character between 0 and \\uFFFF";
        string errorDesc;

        char expectedChar = TestLibrary.Generator.GetChar(-55);
        char actualChar;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            IConvertible converter = expectedChar;
            actualChar = converter.ToChar(null);
            
            if (actualChar != expectedChar)
            {
                errorDesc = string.Format("Character is not \\u{0:x} as expected: Actual(\\u{1:x}", 
                                                        (int)expectedChar, (int)actualChar);
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

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        const string c_TEST_DESC = "PosTest2: Char.MaxValue";
        string errorDesc;

        char expectedChar = char.MaxValue;
        char actualChar;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            IConvertible converter = expectedChar;
            actualChar = converter.ToChar(null);

            if (actualChar != expectedChar)
            {
                errorDesc = string.Format("Character is not \\u{0:x} as expected: Actual(\\u{1:x}",
                                                        (int)expectedChar, (int)actualChar);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "P003";
        const string c_TEST_DESC = "PosTest3: Char.MinValue";
        string errorDesc;

        char expectedChar = char.MinValue;
        char actualChar;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            IConvertible converter = expectedChar;
            actualChar = converter.ToChar(null);

            if (actualChar != expectedChar)
            {
                errorDesc = string.Format("Character is not \\u{0:x} as expected: Actual(\\u{1:x}",
                                                        (int)expectedChar, (int)actualChar);
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}

