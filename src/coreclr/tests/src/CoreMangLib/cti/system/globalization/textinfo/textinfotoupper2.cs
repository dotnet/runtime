// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// System.Globalization.TextInfo.ToUpper(String)
/// </summary>
public class TextInfoToUpper2
{
    public static int Main()
    {
        TextInfoToUpper2 stu1 = new TextInfoToUpper2();
        TestLibrary.TestFramework.BeginTestCase("for Method:System.Globalization.TextInfo.ToUpper(String)");

        if (stu1.RunTests())
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
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        string strA = "HelloWorld!";
        string ActualResult;

        TextInfo textInfo = new CultureInfo("en-US").TextInfo;

        TestLibrary.TestFramework.BeginScenario("PosTest1: normal string ToUpper");
        try
        {   
            ActualResult = textInfo.ToUpper(strA);
            if (ActualResult != "HELLOWORLD!")
            {
                TestLibrary.TestFramework.LogError("001", "normal string ToUpper ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        string strA = string.Empty;
        string ActualResult;

        TextInfo textInfo = new CultureInfo("en-US").TextInfo;


        TestLibrary.TestFramework.BeginScenario("PosTest2: empty string ToUpper");
        try
        {
            ActualResult = textInfo.ToUpper(strA);
            if (ActualResult != string.Empty)
            {
                TestLibrary.TestFramework.LogError("003", "empty string ToUpper ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    
    public bool PosTest3()
    {
        bool retVal = true;
        string strA = "Hello\n\0World\u0009!";
        string ActualResult;

        TextInfo textInfo = new CultureInfo("en-US").TextInfo;

        TestLibrary.TestFramework.BeginScenario("PosTest3: normal string with special symbols '\u0009' and '\0'");
        try
        {
            
            ActualResult = textInfo.ToUpper(strA);
            if (ActualResult != "HELLO\n\0WORLD\t!")
            {
                TestLibrary.TestFramework.LogError("005", "normal string with special symbols ToUpper ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        string strA = "HelloWorld!";
        string ActualResult;

        TextInfo textInfo = new CultureInfo("fr-FR").TextInfo;

        TestLibrary.TestFramework.BeginScenario("PosTest1: normal string ToUpper and TextInfo is French (France) CultureInfo's");
        try
        {
            ActualResult = textInfo.ToUpper(strA);
            if (ActualResult != "HELLOWORLD!")
            {
                TestLibrary.TestFramework.LogError("009", "normal string ToUpper ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        string strA = "Hello\n\0World\u0009!";
        string ActualResult;

        TextInfo textInfo = new CultureInfo("fr-FR").TextInfo;

        TestLibrary.TestFramework.BeginScenario("PosTest6: normal string with special symbols and TextInfo is French (France) CultureInfo's");
        try
        {

            ActualResult = textInfo.ToUpper(strA);
            if (ActualResult != "HELLO\n\0WORLD\t!")
            {
                TestLibrary.TestFramework.LogError("011", "normal string with special symbols ToUpper ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion

    #region NegativeTests
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest1: The string is a null reference";
        const string c_TEST_ID = "N001";

        TextInfo textInfoUS = new CultureInfo("en-US").TextInfo;
        string str = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            textInfoUS.ToUpper(str);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, "ArgumentNullException is not thrown as expected.");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest2: The string is a null reference and TextInfo is French (France) CultureInfo's";
        const string c_TEST_ID = "N001";

        TextInfo textInfoUS = new CultureInfo("fr-FR").TextInfo;
        string str = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            textInfoUS.ToUpper(str);
            TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, "ArgumentNullException is not thrown as expected.");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}

