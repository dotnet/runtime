// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Char.IsSurrogate(Char)  
/// Indicates whether the character at the specified position in a specified string is categorized 
/// as a surrogate.
/// </summary>
public class CharIsSurrogate
{
    private const int c_MIN_STR_LEN = 2;
    private const int c_MAX_STR_LEN = 256;

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

        TestLibrary.TestFramework.LogInformation("[Negaitive]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

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
            string str = new string(ch, 1);
            bool actualResult = char.IsSurrogate(str, 0);
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
            char.IsSurrogate(null, index);
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
            char.IsSurrogate(str, index);
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
            errorDesc += string.Format("\nThe string is {0}, and the index is {1}", str, index);
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
            char.IsSurrogate(str, index);
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
            errorDesc += string.Format("\nThe string is {0}, and the index is {1}", str, index);
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}

