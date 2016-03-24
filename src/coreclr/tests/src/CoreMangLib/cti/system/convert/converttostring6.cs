// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// Convert.ToString(System.Char,System.IFormatProvider)
/// </summary>
public class ConvertToString6
{
    public static int Main()
    {
        ConvertToString6 testObj = new ConvertToString6();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToString(System.Char,System.IFormatProvider)");
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

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest1: Verify value is a random Char and IFormatProvider is a null reference... ";
        string c_TEST_ID = "P001";


        Char charValue = TestLibrary.Generator.GetChar(-55);
        String actualValue = new String(charValue, 1);
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(charValue,provider);
            if (actualValue != resValue)
            {
                String errorDesc = String.Format("String representation of character \\u{0:x} is not the value ", (int)charValue);
                errorDesc += String.Format("{0} as expected: actual({1})", actualValue, resValue);
                errorDesc += "\n IFormatProvider is a nll reference";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest2: Verify value is a random Char and en-US CultureInfo... ";
        string c_TEST_ID = "P002";


        Char charValue = TestLibrary.Generator.GetChar(-55);
        String actualValue = new String(charValue, 1);
        IFormatProvider provider = new CultureInfo("en-US");
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(charValue, provider);
            if (actualValue != resValue)
            {
                String errorDesc = String.Format("String representation of character \\u{0:x} is not the value ", (int)charValue);
                errorDesc += String.Format("{0} as expected: actual({1})", actualValue, resValue);
                errorDesc += "\n IFormatProvider is en-US CultureInfo";
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest3: Verify value is a random Char and fr-FR CultureInfo... ";
        string c_TEST_ID = "P003";


        Char charValue = TestLibrary.Generator.GetChar(-55);
        String actualValue = new String(charValue, 1);
        IFormatProvider provider = new CultureInfo("fr-FR");
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(charValue, provider);
            if (actualValue != resValue)
            {
                String errorDesc = String.Format("String representation of character \\u{0:x} is not the value ", (int)charValue);
                errorDesc += String.Format("{0} as expected: actual({1})", actualValue, resValue);
                errorDesc += "\n IFormatProvider is fr-FR CultureInfo";
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    #endregion
}
