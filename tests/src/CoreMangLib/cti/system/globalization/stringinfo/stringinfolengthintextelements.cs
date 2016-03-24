// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// System.Globalization.StringInfo.LengthInTextElements
/// </summary>
public class StringInfoLengthInTextElements
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: The mothod should return the number of base character in the current stringInfo object");

        try
        {
            retVal = VerificationHelper("\u4f00\u302a\ud800\udc00\u4f01", 3, "001.1");
            retVal = VerificationHelper("abcdefgh", 8, "001.2");
            retVal = VerificationHelper("zj\uDBFF\uDFFFlk", 5, "001.3");
            retVal = VerificationHelper("!@#$%^&", 7, "001.4");
            retVal = VerificationHelper("!\u20D1bo\uFE22\u20D1\u20EB|", 4, "001.5");
            retVal = VerificationHelper("1\uDBFF\uDFFF@\uFE22\u20D1\u20EB9", 4, "001.6");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: The string in stringinfo is white space or empty string");

        try
        {
            retVal = VerificationHelper("   ", 3, "001.1");
            retVal = VerificationHelper(string.Empty, 0, "001.2");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    #endregion
    #endregion

    public static int Main()
    {
        StringInfoLengthInTextElements test = new StringInfoLengthInTextElements();

        TestLibrary.TestFramework.BeginTestCase("StringInfoLengthInTextElements");

        if (test.RunTests())
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
    #region Private Methods
    private bool VerificationHelper(string str, int expected, string errorno)
    {
        bool retVal = true;

        StringInfo stringInfo = new StringInfo(str);
        int result = stringInfo.LengthInTextElements;
        if (result != expected)
        {
            TestLibrary.TestFramework.LogError(errorno, "The result is not the value as expected,The actual is: " + result + ", the desire is: " + expected);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
