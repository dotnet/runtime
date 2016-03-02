// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// char.IsLetterOrDigit(string, int)  
/// Indicates whether the character at the specified position in a specified string is categorized 
/// as a letter or decimal digit character. 
/// </summary>
public class CharIsLetterOrDigit
{
    private const int c_MIN_STR_LEN = 2;
    private const int c_MAX_STR_LEN = 256;

    private static readonly char[] r_decimalDigits = 
    {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
    };

    public static int Main()
    {
        CharIsLetterOrDigit testObj = new CharIsLetterOrDigit();

        TestLibrary.TestFramework.BeginTestCase("for method: char.IsLetterOrDigit(string, int)");
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
        retVal = PosTest3() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negaitive]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

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
        int index = TestLibrary.Generator.GetInt32(-55) % r_decimalDigits.Length;
        char ch = r_decimalDigits[index];

        return this.DoTest("PosTest2: Decimal digit character.",
                                  "P002", "003", "004", ch, true);
    }

    public bool PosTest3()
    {
        //Generate a non  character for validate
        char ch = '\n';

        return this.DoTest("PosTest3: Not decimal digit nor letter character.",
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
            bool actualResult = char.IsLetterOrDigit(ch);
            if (expectedResult != actualResult)
            {
                if (expectedResult)
                {
                    errorDesc = string.Format("Character \\u{0:x} should belong to letter or decimal digit characters.", (int)ch);
                }
                else
                {
                    errorDesc = string.Format("Character \\u{0:x} does not belong to letter or decimal digit characters.", (int)ch);
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

    #region Negative tests
    //ArgumentNullException
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "N001";
        const string c_TEST_DESC = "NegTest1: String is a null reference (Nothing in Visual Basic).";
        string errorDesc;

        int index = 0;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            index = TestLibrary.Generator.GetInt32(-55);
            char.IsLetterOrDigit(null, index);
            errorDesc = "ArgumentNullException is not thrown as expected, index is " + index;
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e + "\n Index is " + index;
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    //ArgumentOutOfRangeException
    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "N002";
        const string c_TEST_DESC = "NegTest2: Index is too great.";
        string errorDesc;

        string str = string.Empty;
        int index = 0;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
            index = str.Length + TestLibrary.Generator.GetInt16(-55);
            index = str.Length;
            char.IsLetterOrDigit(str, index);
            errorDesc = "ArgumentOutOfRangeException is not thrown as expected";
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nThe character is \\u{0:x}", str[index]);
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "N003";
        const string c_TEST_DESC = "NegTest3: Index is a negative value";
        string errorDesc;

        string str = string.Empty;
        int index = 0;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
            index = -1 * (TestLibrary.Generator.GetInt16(-55));
            char.IsLetterOrDigit(str, index);
            errorDesc = "ArgumentOutOfRangeException is not thrown as expected";
            TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nThe character is \\u{0:x}", str[index]);
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}

