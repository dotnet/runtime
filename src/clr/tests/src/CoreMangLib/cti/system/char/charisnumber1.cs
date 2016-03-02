// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Char.IsNumber(Char)  
/// Indicates whether the specified Unicode character is categorized as a number. 
/// </summary>
public class CharIsNumber
{
    private static readonly char[] r_decimalDigits = 
    {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
    };

    public static int Main()
    {
        CharIsNumber testObj = new CharIsNumber();

        TestLibrary.TestFramework.BeginTestCase("for method: Char.IsNumber(Char)");
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
    #region Positive tests
    public bool PosTest1()
    {
        //Generate the character for validate
        int index = TestLibrary.Generator.GetInt32(-55) % r_decimalDigits.Length;
        char ch = r_decimalDigits[index];

        return this.DoTest("PosTest1: Number character",
                                  "P001", "001", "002", ch, true);
    }

    public bool PosTest2()
    {
        //Generate a non  character for validate
        char ch = 'A';
        return this.DoTest("PosTest2: Non-number character.",
                                  "P002", "003", "004", ch, false);
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
            bool actualResult = char.IsNumber(ch);
            if (expectedResult != actualResult)
            {
                if (expectedResult)
                {
                    errorDesc = string.Format("Character \\u{0:x} should belong to number.", (int)ch);
                }
                else
                {
                    errorDesc = string.Format("Character \\u{0:x} does not belong to number.", (int)ch);
                }

                TestLibrary.TestFramework.LogError(errorNum1 + " TestId-" + testId, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nCharacter is \\u{0:x}", ch);
            TestLibrary.TestFramework.LogError(errorNum2 + " TestId-" + testId, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}

