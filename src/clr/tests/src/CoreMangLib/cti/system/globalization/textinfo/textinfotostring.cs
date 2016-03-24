// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// System.Globalization.TextInfo.ToString()
/// </summary>
public class TextInfoToString
{
    public static int Main()
    {
        TextInfoToString testObj = new TextInfoToString();
        TestLibrary.TestFramework.BeginTestCase("for Method:System.Globalization.TextInfo.ToString()");

        if (testObj.RunTests())
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

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Verify the en-US TextInfo";
        const string c_TEST_ID = "P001";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        TextInfo textInfoUS = new CultureInfo("en-US").TextInfo;
        String textinfoStr = "TextInfo - en-US";
        try
        {

            if (textinfoStr != textInfoUS.ToString())
            {
                string errorDesc = "Value is not " + textinfoStr + " as expected: Actual(" + textInfoUS.ToString() + ")";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2: Verify the fr-FR CultureInfo's TextInfo";
        const string c_TEST_ID = "P002";


        TextInfo    textInfoFrance = new CultureInfo("fr-FR").TextInfo;
        String textinfoStr = "TextInfo - fr-FR";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (textinfoStr != textInfoFrance.ToString())
            {
                string errorDesc = "Value is not " + textinfoStr + " as expected: Actual(" + textInfoFrance.ToString() + ")";
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}

