// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// System.Globalization.TextInfo.ToUpper(Char)
/// </summary>
public class TextInfoToUpper1
{
    public static int Main()
    {
        TextInfoToUpper1 testObj = new TextInfoToUpper1();

        TestLibrary.TestFramework.BeginTestCase("for method: System.Globalization.TextInfo.ToUpper(Char)");
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
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: uppercase character";
        string errorDesc;

        char ch = 'A';
        char expectedChar = ch;
        TextInfo textInfo = new CultureInfo("en-US").TextInfo;


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
           
            char actualChar = textInfo.ToUpper(ch);
            if (actualChar != expectedChar)
            {
                errorDesc = string.Format("Uppercase of character \\u{0:x} is not the value ", (int)ch);
                errorDesc += string.Format("\\u{0:x} as expected: actual(\\u{1:x}", (int)expectedChar, (int)actualChar);
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
        const string c_TEST_DESC = "PosTest2: lowercase character";
        string errorDesc;

        char ch = 'a';
        char expectedChar = 'A';
        TextInfo textInfo = new CultureInfo("en-US").TextInfo;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            
            char actualChar = textInfo.ToUpper(ch);
            if (actualChar != expectedChar)
            {
                errorDesc = string.Format("Uppercase of character \\u{0:x} is not the value ", (int)ch);
                errorDesc += string.Format("\\u{0:x} as expected: actual(\\u{1:x}", (int)expectedChar, (int)actualChar);
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
        const string c_TEST_DESC = "PosTest3: non-alphabetic character";
        string errorDesc;

        char ch = Convert.ToChar(TestLibrary.Generator.GetInt16(-55) % 10 + '0');
        char expectedChar = ch;
        TextInfo textInfo = new CultureInfo("en-US").TextInfo;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            
            char actualChar = textInfo.ToUpper(ch);
            if (actualChar != expectedChar)
            {
                errorDesc = string.Format("Uppercase of character \\u{0:x} is not the value ", (int)ch);
                errorDesc += string.Format("\\u{0:x} as expected: actual(\\u{1:x}", (int)expectedChar, (int)actualChar);
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
        const string c_TEST_DESC = "PosTest4: uppercase character and TextInfo is french CultureInfo's";
        string errorDesc;

        char ch = 'G';
        char expectedChar = ch;
        TextInfo textInfo = new CultureInfo("fr-FR").TextInfo;


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {

            char actualChar = textInfo.ToUpper(ch);
            if (actualChar != expectedChar)
            {
                errorDesc = string.Format("Uppercase of character \\u{0:x} is not the value ", (int)ch);
                errorDesc += string.Format("\\u{0:x} as expected: actual(\\u{1:x}", (int)expectedChar, (int)actualChar);
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

    public bool PosTest5()
    {
        bool retVal = true;

        const string c_TEST_ID = "P005";
        const string c_TEST_DESC = "PosTest5: lowercase character and TextInfo is french(France) CultureInfo's";
        string errorDesc;

        char ch = 'g';
        char expectedChar = 'G';
        TextInfo textInfo = new CultureInfo("fr-FR").TextInfo;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {

            char actualChar = textInfo.ToUpper(ch);
            if (actualChar != expectedChar)
            {
                errorDesc = string.Format("Uppercase of character \\u{0:x} is not the value ", (int)ch);
                errorDesc += string.Format("\\u{0:x} as expected: actual(\\u{1:x}", (int)expectedChar, (int)actualChar);
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

}

