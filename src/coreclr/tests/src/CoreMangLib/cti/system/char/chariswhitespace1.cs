// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Char.IsWhiteSpace(Char)  
/// Indicates whether the specified Unicode character is categorized as a whitespace. 
/// </summary>
public class CharIsWhiteSpace
{
    private static readonly char[] r_whiteSpaceChars =
    {
        (char)0x0009, (char)0x000A, (char)0x000B, (char)0x000C, (char)0x000D, 
        (char)0x0020, 
        (char)0x0085, (char)0x00A0,
        (char)0x1680, (char)0x180E, //compatibility with desktop
        (char)0x2000, (char)0x2001, (char)0x2002, (char)0x2003, (char)0x2004, (char)0x2005, 
        (char)0x2006, (char)0x2007, (char)0x2008, (char)0x2009, (char)0x200A, //(char)0x200B,
        (char)0x2028, (char)0x2029,
        (char)0x202F, (char)0x205F, //compatibility with desktop
        (char)0x3000, //(char)0xFEFF
    };

    public static int Main()
    {
        CharIsWhiteSpace testObj = new CharIsWhiteSpace();

        TestLibrary.TestFramework.BeginTestCase("for method: Char.IsWhiteSpace(Char)");
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
        int index = TestLibrary.Generator.GetInt32(-55) % r_whiteSpaceChars.Length;
        char ch = r_whiteSpaceChars[index];

        return this.DoTest("PosTest1: Whitespace character",
                                  "P001", "001", "002", ch, true);
    }

    public bool PosTest2()
    {
        //Generate a non separator character for validate
        char ch = TestLibrary.Generator.GetCharLetter(-55);
        return this.DoTest("PosTest2: Non-whitespace character.",
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
            bool actualResult = char.IsWhiteSpace(ch);
            if (expectedResult != actualResult)
            {
                if (expectedResult)
                {
                    errorDesc = string.Format("Character \\u{0:x} should belong to whitespace.", (int)ch);
                }
                else
                {
                    errorDesc = string.Format("Character \\u{0:x} does not belong to whitespace.", (int)ch);
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

