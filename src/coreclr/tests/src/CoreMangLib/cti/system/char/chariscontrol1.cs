// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Char.IsControl(Char)
/// Indicates whether the specified Unicode character is categorized as a control character. 
/// </summary>
public class CharIsControl
{
    public static int Main()
    {
        CharIsControl testObj = new CharIsControl();

        TestLibrary.TestFramework.BeginTestCase("for method: Char.GetUnicodeCategory(Char)");
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
        retVal = PosTest2() && retVal;

        return retVal;
    }

    #region Positve tests
    public bool PosTest1()
    {
        //Generate the character for validate
        char ch = TestLibrary.Generator.GetCharLetter(-55);

        return this.DoTest("PosTest1: Non control character.",
                                  "P001", "001", "002", ch,
                                  false);
    }

    public bool PosTest2()
    {
        //Generate the character for validate
        char ch = '\n';

        return this.DoTest("PosTest2: Control character.",
                                  "P002", "003", "004", ch,
                                  true);
    }
    #endregion

    #region Helper method for positive tests
    private bool DoTest(string testDesc,
                                string testId,
                                string errorNum1,
                                string errorNum2,
                                char ch,
                                bool expectedResult)
    {
        bool retVal = true;
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(testDesc);
        try
        {
            bool actualResult = char.IsControl(ch);
            if (expectedResult != actualResult)
            {
                if (expectedResult)
                {
                    errorDesc = string.Format("Character \\u{0:x} should belong to control characters.", (int)ch);
                }
                else
                {
                    errorDesc = string.Format("Character \\u{0:x} does not belong to  control characters.", (int)ch);
                }
                
                TestLibrary.TestFramework.LogError(errorNum1+ " TestId-" + testId, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nThe character is \\u{0:x}", (int)ch);
            TestLibrary.TestFramework.LogError(errorNum2 + " TestId-" + testId, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}

