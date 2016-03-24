// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// StringBuilder.Chars Property 
/// Gets or sets the character at the specified character position in this instance. 
/// </summary>
public class StringBuilderChars
{
    private const int c_MIN_STR_LEN = 1;
    private const int c_MAX_STR_LEN = 260;

    private const int c_MAX_CAPACITY = Int16.MaxValue;

    public static int Main()
    {
        StringBuilderChars testObj = new StringBuilderChars();

        TestLibrary.TestFramework.BeginTestCase("for property: StringBuilder.Chars");
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: Get the Chars property";
        string errorDesc;

        StringBuilder sb;
        char actualChar, expectedChar;
        int index;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
            sb = new StringBuilder(str);

            index = TestLibrary.Generator.GetInt32(-55) % str.Length;

            expectedChar = str[index];
            actualChar = sb[index];

            if (actualChar != expectedChar)
            {
                errorDesc = "Character of current StringBuilder " + sb + " at sepcifed index " + index
                    + " is not the value ";
                errorDesc += string.Format("{0} as expected: actual({1})", expectedChar, actualChar);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        const string c_TEST_DESC = "PosTest2: Set the Chars property";
        string errorDesc;

        StringBuilder sb;
        char actualChar, expectedChar;
        int index;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
            sb = new StringBuilder(str);

            index = TestLibrary.Generator.GetInt32(-55) % str.Length;

            expectedChar = TestLibrary.Generator.GetChar(-55);
            sb[index] = expectedChar;
            actualChar = sb[index];

            if (actualChar != expectedChar)
            {
                errorDesc = "Character of current StringBuilder " + sb + " at sepcifed index " + index
                    + " is not the value ";
                errorDesc += string.Format("{0} as expected: actual({1})", expectedChar, actualChar);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Negative tests
    //IndexOutOfRangeException
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "N001";
        const string c_TEST_DESC = "NegTest1: index is greater than or equal the current length of this instance while getting a character.";
        string errorDesc;

        StringBuilder sb;
        int index, currentInstanceLength;

        string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
        sb = new StringBuilder(str);
        currentInstanceLength = str.Length;
        index = currentInstanceLength + 
            TestLibrary.Generator.GetInt32(-55) % (int.MaxValue - currentInstanceLength);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            char ch = sb[index];
            errorDesc = "IndexOutOfRangeException is not thrown as expected.";
            errorDesc += string.Format("\nString value of StringBuilder is {0}", str);
            errorDesc += string.Format("\nCurrent length of instance is {0}, index specified is {1}", 
                               currentInstanceLength, index);
            TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (IndexOutOfRangeException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nString value of StringBuilder is {0}", str);
            errorDesc += string.Format("\nCurrent length of instance is {0}, index specified is {1}",
                               currentInstanceLength, index);
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "N002";
        const string c_TEST_DESC = "NegTest2: index is less than zero while getting a character.";
        string errorDesc;

        StringBuilder sb;
        int index, currentInstanceLength;

        string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
        sb = new StringBuilder(str);
        currentInstanceLength = str.Length;
        index = -1 * TestLibrary.Generator.GetInt32(-55) - 1;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            char ch = sb[index];
            errorDesc = "IndexOutOfRangeException is not thrown as expected.";
            errorDesc += string.Format("\nString value of StringBuilder is {0}", str);
            errorDesc += string.Format("\nCurrent length of instance is {0}, index specified is {1}",
                               currentInstanceLength, index);
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (IndexOutOfRangeException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nString value of StringBuilder is {0}", str);
            errorDesc += string.Format("\nCurrent length of instance is {0}, index specified is {1}",
                               currentInstanceLength, index);
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    //ArgumentOutOfRangeException
    public bool NegTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "N003";
        const string c_TEST_DESC = "NegTest3: index is greater than or equal the current length of this instance while setting a character.";
        string errorDesc;

        StringBuilder sb;
        int index, currentInstanceLength;

        string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
        sb = new StringBuilder(str);
        currentInstanceLength = str.Length;
        index = currentInstanceLength +
            TestLibrary.Generator.GetInt32(-55) % (int.MaxValue - currentInstanceLength);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            char ch = TestLibrary.Generator.GetChar(-55);
            sb[index] =ch;
            errorDesc = "ArgumentOutOfRangeException is not thrown as expected.";
            errorDesc += string.Format("\nString value of StringBuilder is {0}", str);
            errorDesc += string.Format("\nCurrent length of instance is {0}, index specified is {1}",
                               currentInstanceLength, index);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nString value of StringBuilder is {0}", str);
            errorDesc += string.Format("\nCurrent length of instance is {0}, index specified is {1}",
                               currentInstanceLength, index);
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        const string c_TEST_ID = "N004";
        const string c_TEST_DESC = "NegTest4: index is less than zero while setting a character.";
        string errorDesc;

        StringBuilder sb;
        int index, currentInstanceLength;

        string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
        sb = new StringBuilder(str);
        currentInstanceLength = str.Length;
        index = -1 * TestLibrary.Generator.GetInt32(-55) - 1;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            char ch = TestLibrary.Generator.GetChar(-55);
            sb[index] = ch;
            errorDesc = "ArgumentOutOfRangeException is not thrown as expected.";
            errorDesc += string.Format("\nString value of StringBuilder is {0}", str);
            errorDesc += string.Format("\nCurrent length of instance is {0}, index specified is {1}",
                               currentInstanceLength, index);
            TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nString value of StringBuilder is {0}", str);
            errorDesc += string.Format("\nCurrent length of instance is {0}, index specified is {1}",
                               currentInstanceLength, index);
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
