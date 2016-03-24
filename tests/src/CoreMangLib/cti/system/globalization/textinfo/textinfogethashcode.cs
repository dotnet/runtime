// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// System.Globalization.TextInfo.GetHashCode()
/// </summary>
public class TextInfoGetHashCode
{
    public static int Main()
    {
        TextInfoGetHashCode testObj = new TextInfoGetHashCode();
        TestLibrary.TestFramework.BeginTestCase("for Method:System.Globalization.TextInfo.GetHashCode()");

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
        const string c_TEST_DESC = "PosTest1: Verify the TextInfo equals original TextInfo. ";
        const string c_TEST_ID = "P001";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        CultureInfo ci = new CultureInfo("en-US");
        CultureInfo ci2 = new CultureInfo("en-US");
        object textInfo = ci2.TextInfo;
       
        try
        {
            int originalHC = ci.TextInfo.GetHashCode();
            int clonedHC = (textInfo as TextInfo).GetHashCode();
            if (originalHC != clonedHC)
            {
                string errorDesc = "the cloned TextInfo'HashCode should equal original TextInfo's HashCode.";
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
        const string c_TEST_DESC = "PosTest2: Verify the TextInfo is not same  CultureInfo's . ";
        const string c_TEST_ID = "P002";


        TextInfo textInfoFrance = new CultureInfo("fr-FR").TextInfo;
        TextInfo textInfoUS = new CultureInfo("en-US").TextInfo;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int franceHashCode = textInfoFrance.GetHashCode();
            int usHashCode = textInfoUS.GetHashCode();
            if (franceHashCode == usHashCode)
            {
                string errorDesc = "the differente TextInfo's HashCode should not equal. ";
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

