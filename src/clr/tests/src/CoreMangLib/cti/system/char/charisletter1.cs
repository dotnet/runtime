using System;

/// <summary>
/// Char.IsLetter(Char)  
/// Indicates whether the specified Unicode character is categorized as a letter. 
/// </summary>
public class CharIsLetter
{
    public static int Main()
    {
        CharIsLetter testObj = new CharIsLetter();

        TestLibrary.TestFramework.BeginTestCase("for method: Char.IsLetter(Char)");
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
        char ch = 'a';

        return this.DoTest("PosTest1: Letter character",
                                  "P001", "001", "002", ch, true);
    }

    public bool PosTest2()
    {
        //Generate a non  character for validate
        char ch = TestLibrary.Generator.GetCharNumber(-55);
        return this.DoTest("PosTest2: Non letter character.", 
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
            bool actualResult = char.IsLetter(ch);
            if (expectedResult != actualResult)
            {
                if (expectedResult)
                {
                    errorDesc = string.Format("Character \\u{0:x} should belong to letter characters.", (int)ch);
                }
                else
                {
                    errorDesc = string.Format("Character \\u{0:x} does not belong to letter characters.", (int)ch);
                }
                
                TestLibrary.TestFramework.LogError(errorNum1+ " TestId-" + testId, errorDesc);
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

