// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Char.IsSurrogatePair(string, index)
/// Note: This method is new in the .NET Framework version 2.0. 
/// Indicates whether two adjacent Char objects at a specified position in a string form a surrogate pair. 
/// </summary>
public class CharIsSurrogatePair
{
    private const int c_MIN_STR_LEN = 2;
    private const int c_MAX_STR_LEN = 256;

    private const char c_HIGH_SURROGATE_START = '\ud800';
    private const char c_HIGH_SURROGATE_END = '\udbff';
    private const char c_LOW_SURROGATE_START = '\udc00';
    private const char c_LOW_SURROGATE_END = '\udfff';

    public static int Main()
    {
        CharIsSurrogatePair testObj = new CharIsSurrogatePair();

        TestLibrary.TestFramework.BeginTestCase("for method: Char.IsSurrogatePair(string, index)");
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

        TestLibrary.TestFramework.LogInformation("[Negaitive]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }
    #region Positive tests
    public bool PosTest1()
    {
        char chA;
        char chB;
        //Generate a high surrogate character for validate
        int count = (int)c_HIGH_SURROGATE_END - (int)c_HIGH_SURROGATE_START + 1;
        int offset = TestLibrary.Generator.GetInt32(-55) % count;
        chA = (char)((int)c_HIGH_SURROGATE_START + offset);

        //Generate a character not low surrogate for validate
        count = (int)c_LOW_SURROGATE_END - (int)c_LOW_SURROGATE_START + 1;
        offset = TestLibrary.Generator.GetInt32(-55) % count;
        chB = (char)((int)c_LOW_SURROGATE_START + offset);

        return this.DoTest("PosTest1: First character is high surrogate, and the second is low surrogate",
                                  "P001", "001", "002", chA, chB, true);
    }

    public bool PosTest2()
    {
        char chA;
        char chB;
        //Generate a high surrogate character for validate
        int count = (int)c_HIGH_SURROGATE_END - (int)c_HIGH_SURROGATE_START + 1;
        int offset = TestLibrary.Generator.GetInt32(-55) % count;
        chA = (char)((int)c_HIGH_SURROGATE_START + offset);

        //Generate a character not low surrogate for validate
        int i = TestLibrary.Generator.GetInt32(-55) & 0x00000001;
        if (0 == i)
        {
            chB = (char)(TestLibrary.Generator.GetInt32(-55) % ((int)c_LOW_SURROGATE_START));
        }
        else
        {
            chB = (char)((int)c_LOW_SURROGATE_END + 1 + 
                TestLibrary.Generator.GetInt32(-55) % ((int)char.MaxValue - (int)c_LOW_SURROGATE_END));
        }

        return this.DoTest("PosTest2: First character is high surrogate, but the second is not low surrogate",
                                  "P002", "003", "004", chA, chB, false);
    }

    public bool PosTest3()
    {
        char chA;
        char chB;
        //Generate a character not high surrogate for validate
        int i = TestLibrary.Generator.GetInt32(-55) & 0x00000001;
        if (0 == i)
        {
            chA = (char)(TestLibrary.Generator.GetInt32(-55) % ((int)c_HIGH_SURROGATE_START));
        }
        else
        {
            chA = (char)((int)c_HIGH_SURROGATE_END + 1 +
                   TestLibrary.Generator.GetInt32(-55) % ((int)char.MaxValue - (int)c_HIGH_SURROGATE_END));
        }

        //Generate a low surrogate character for validate
        int count = (int)c_LOW_SURROGATE_END - (int)c_LOW_SURROGATE_START + 1;
        int offset = TestLibrary.Generator.GetInt32(-55) % count;
        chB = (char)((int)c_LOW_SURROGATE_START + offset);

        return this.DoTest("PosTest2: Second character is low surrogate, but the first is not high surrogate",
                                  "P003", "005", "006", chA, chB, false);
    }

    public bool PosTest4()
    {
        char chA;
        char chB;
        //Generate a character not high surrogate for validate
        int i = TestLibrary.Generator.GetInt32(-55) & 0x00000001;
        if (0 == i)
        {
            chA = (char)(TestLibrary.Generator.GetInt32(-55) % ((int)c_HIGH_SURROGATE_START));
        }
        else
        {
            chA = (char)((int)c_HIGH_SURROGATE_END + 1 +
                   TestLibrary.Generator.GetInt32(-55) % ((int)char.MaxValue - (int)c_HIGH_SURROGATE_END));
        }

        //Generate a character not low surrogate for validate
        i = TestLibrary.Generator.GetInt32(-55) & 0x00000001;
        if (0 == i)
        {
            chB = (char)(TestLibrary.Generator.GetInt32(-55) % ((int)c_LOW_SURROGATE_START));
        }
        else
        {
            chB = (char)((int)c_LOW_SURROGATE_END + 1 +
                TestLibrary.Generator.GetInt32(-55) % ((int)char.MaxValue - (int)c_LOW_SURROGATE_END));
        }

        return this.DoTest("PosTest2: Both the first character and the second are invalid",
                                  "P004", "007", "008", chA, chB, false);
    }
    #endregion

    #region Helper method for positive tests
    private bool DoTest(string testDesc,
                                string testId,
                                string errorNum1,
                                string errorNum2,
                                char chA, 
                                char chB,
                                bool expectedResult)
    {
        bool retVal = true;
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(testDesc);
        try
        {
            string str = string.Empty + chA + chB;
            bool actualResult = char.IsSurrogatePair(str, 0);
            if (expectedResult != actualResult)
            {
                if (expectedResult)
                {
                    errorDesc = string.Format("Character \\u{0:x} and \\u{1:x} should belong to surrogate pair.", (int)chA, (int)chB);
                }
                else
                {
                    errorDesc = string.Format("Character  \\u{0:x} and \\u{1:x} does not belong to surrogate pair.", (int)chA, (int)chB);
                }

                TestLibrary.TestFramework.LogError(errorNum1 + " TestId-" + testId, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nCharacter is \\u{0:x} and \\u{1:x}", chA, chB);
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
            char.IsPunctuation(null, index);
            errorDesc = "ArgumentNullException is not thrown as expected, index is " + index;
            TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e + "\n Index is " + index;
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
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
            char.IsPunctuation(str, index);
            errorDesc = "ArgumentOutOfRangeException is not thrown as expected";
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nThe string is {0}, and the index is {1}", str, index);
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
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
            char.IsPunctuation(str, index);
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
    #endregion
}

