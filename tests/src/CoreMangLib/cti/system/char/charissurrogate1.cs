// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Char.IsSurrogate(Char)  
/// Indicates whether the specified Unicode character is categorized as a surrogate. 
/// </summary>
public class CharIsSurrogate
{
    private const char c_HIGH_SURROGATE_START = '\ud800';
    private const char c_HIGH_SURROGATE_END = '\udbff';
    private const char c_LOW_SURROGATE_START = '\udc00';
    private const char c_LOW_SURROGATE_END = '\udfff';

    public static int Main()
    {
        CharIsSurrogate testObj = new CharIsSurrogate();

        TestLibrary.TestFramework.BeginTestCase("for method: Char.IsSurrogate(Char)");
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
        char ch;
        //Generate a low surrogate character for validate
        int count = (int)c_LOW_SURROGATE_END - (int)c_LOW_SURROGATE_START + 1;
        int offset = TestLibrary.Generator.GetInt32(-55) % count;
        ch = (char)((int)c_LOW_SURROGATE_START + offset);

        return this.DoTest("PosTest1: Low surrogate character",
                                  "P001", "001", "002", ch, true);
    }

    public bool PosTest2()
    {
        char ch;
        //Generate a hign surrogate character for validate
        int count = (int)c_HIGH_SURROGATE_END - (int)c_HIGH_SURROGATE_START + 1;
        int offset = TestLibrary.Generator.GetInt32(-55) % count;
        ch = (char)((int)c_HIGH_SURROGATE_START + offset);

        return this.DoTest("PosTest2: Hign surrogate character.",
                                  "P002", "003", "004", ch, true);
    }

    public bool PosTest3()
    {
        //Generate a non surrogate character for validate
        char ch = TestLibrary.Generator.GetCharLetter(-55);
        return this.DoTest("PosTest3: Non-surrogate character.",
                                  "P003", "005", "006", ch, false);
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
            bool actualResult = char.IsSurrogate(ch);
            if (expectedResult != actualResult)
            {
                if (expectedResult)
                {
                    errorDesc = string.Format("Character \\u{0:x} should belong to surrogate.", (int)ch);
                }
                else
                {
                    errorDesc = string.Format("Character \\u{0:x} does not belong to surrogate.", (int)ch);
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

