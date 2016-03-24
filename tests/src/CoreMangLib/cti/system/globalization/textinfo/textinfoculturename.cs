// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// System.Globalization.TextInfo.CultureName
/// </summary>
public class TextInfoCultureName
{
    public static int Main()
    {
        TextInfoCultureName testObj = new TextInfoCultureName();
        TestLibrary.TestFramework.BeginTestCase("for Property:System.Globalization.TextInfo.CultureInfo");

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

        CultureInfo ci = new CultureInfo("en-US");
        TextInfo textInfoUS = ci.TextInfo;
        String cultureName = ci.Name;
        try
        {

            if (cultureName != textInfoUS.CultureName)
            {
                string errorDesc = "Value is not " + cultureName + " as expected: Actual(" + textInfoUS.CultureName + ")";
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


        CultureInfo ci = new CultureInfo("fr-FR");
        TextInfo textInfoFrance = ci.TextInfo;
        String cultureName = ci.Name;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (cultureName != textInfoFrance.CultureName)
            {
                string errorDesc = "Value is not " + cultureName + " as expected: Actual(" + textInfoFrance.CultureName + ")";
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

