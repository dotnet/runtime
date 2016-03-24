// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// StringBuilder.ToString() 
/// Converts the value of this instance to a String. 
/// </summary>
public class StringBuilderToString
{
    private const int c_MIN_STR_LEN = 1;
    private const int c_MAX_STR_LEN = 260;

    public static int Main()
    {
        StringBuilderToString testObj = new StringBuilderToString();

        TestLibrary.TestFramework.BeginTestCase("for method: StringBuilder.ToString()");
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
        const string c_TEST_DESC = "PosTest1: Random string ";
        string errorDesc;

        StringBuilder sb;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string expectedStr = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
            sb = new StringBuilder(expectedStr);
            string actualStr = sb.ToString();
            if (actualStr != expectedStr || Object.ReferenceEquals(actualStr, expectedStr))
            {
                errorDesc = " String value of StringBuilder is not the value ";
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

