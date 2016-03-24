// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Convert.ToString(System.Char)
/// </summary>
public class ConvertToString5
{
    public static int Main()
    {
        ConvertToString5 testObj = new ConvertToString5();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToString(System.Char)");
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

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest1: Verify value is a random Char... ";
        string c_TEST_ID = "P001";


        Char charValue = TestLibrary.Generator.GetChar(-55);
        String actualValue = new String(charValue, 1);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(charValue);
            if (actualValue != resValue)
            {
                String errorDesc = String.Format("String representation of character \\u{0:x} is not the value ", (int)charValue);
                errorDesc += String.Format("{0} as expected: actual({1})", actualValue, resValue);
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

   
    #endregion
}
