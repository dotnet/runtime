// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Char.MinValue Field  
/// Represents the largest possible value of a Char. This field is constant. 
/// </summary>
public class CharMinValue
{
    public static int Main()
    {
        CharMinValue testObj = new CharMinValue();

        TestLibrary.TestFramework.BeginTestCase("for field: Char.MinValue");
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

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: ";
        string errorDesc;

        const char c_MIN_CHAR = '\u0000';

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            char actualChar = char.MinValue;
            if (actualChar != c_MIN_CHAR)
            {
                errorDesc = "Field char.MinValue is not " + c_MIN_CHAR + " as expected: Actual(" + actualChar + ")";
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
}

