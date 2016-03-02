// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Char.ToString()  
/// Converts the value of this instance to its equivalent string representation. 
/// </summary>
public class CharToString
{
    public static int Main()
    {
        CharToString testObj = new CharToString();

        TestLibrary.TestFramework.BeginTestCase("for method: Char.ToString()");
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
        const string c_TEST_DESC = "PosTest1: Random character";
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            char ch = TestLibrary.Generator.GetChar(-55);
            string expectedStr = new string(ch, 1);
            string actualStr = ch.ToString();
            if (actualStr != expectedStr)
            {
                errorDesc = string.Format("String representation of character \\u{0:x} is not the value ", (int)ch);
                errorDesc += string.Format("{0} as expected: actual({1})", expectedStr, actualStr);
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

