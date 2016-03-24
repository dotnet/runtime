// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Char.Equals(Char)  
/// Note: This method is new in the .NET Framework version 2.0. 
/// Returns a value indicating whether this instance is equal to the specified Char object.   
/// </summary>
public class CharEquals
{
    public static int Main()
    {
        CharEquals testObj = new CharEquals();

        TestLibrary.TestFramework.BeginTestCase("for method: Char.Equals(Char)");
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
        retVal = PosTest4() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = @"PosTest1: char.MaxValue vs '\uFFFF'";
        string errorDesc;

        const char c_MAX_CHAR = '\uFFFF';

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            char actualChar = char.MaxValue;
            bool result = actualChar.Equals(c_MAX_CHAR);
            if (!result)
            {
                errorDesc = "Char.MaxValue is not " + c_MAX_CHAR + " as expected: Actual(" + actualChar + ")";
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
        const string c_TEST_DESC = @"PosTest2: char.MinValue vs '\u0000'";
        string errorDesc;

        const char c_MIN_CHAR = '\u0000';

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            char actualChar = char.MinValue;
            bool result = actualChar.Equals(c_MIN_CHAR);
            if (!result)
            {
                errorDesc = "char.MinValue is not " + c_MIN_CHAR + " as expected: Actual(" + actualChar + ")";
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
        const string c_TEST_DESC = "PosTest3: char.MaxValue vs char.MinValue";
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            bool expectedValue = false;
            bool actualValue = char.MaxValue.Equals(char.MinValue);
            if (actualValue != expectedValue)
            {
                errorDesc = @"char.MaxValue('\uFFFF') does not equal char.MinValue('\u0000')";
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

    public bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_ID = "P004";
        const string c_TEST_DESC = "PosTest4: equality of two random charaters";
        string errorDesc;

        char chA, chB;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            chA = TestLibrary.Generator.GetChar(-55);
            chB = TestLibrary.Generator.GetChar(-55);
            bool expectedValue = (int)chA == (int)chB;
            bool actualValue = chA.Equals(chB);
            if (actualValue != expectedValue)
            {
                errorDesc = string.Format("The equality of character \'\\u{0:x}\' against character \'\\u{1:x}\' is not {2} as expected: Actual({3})",
                    (int)chA, (int)chB, expectedValue, actualValue);
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
}

