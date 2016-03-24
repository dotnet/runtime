// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// StringBuilder.ToString(int, int)
/// Converts the value of a substring of this instance to a String.  
/// </summary>
public class StringBuilderToString
{
    private const int c_MIN_STR_LEN = 1;
    private const int c_MAX_STR_LEN = 260;

    public static int Main()
    {
        StringBuilderToString testObj = new StringBuilderToString();

        TestLibrary.TestFramework.BeginTestCase("for method: StringBuilder.ToString(int, int)");
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: Random start index and random length ";
        string errorDesc;

        StringBuilder sb;
        int startIndex;
        int length;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
            sb = new StringBuilder(str);
            startIndex = TestLibrary.Generator.GetInt32(-55) % str.Length;
            length = TestLibrary.Generator.GetInt32(-55) % (str.Length - startIndex) + 1;
            string expectedStr = str.Substring(startIndex, length);
            string actualStr = sb.ToString(startIndex, length);

            if (actualStr != expectedStr || Object.ReferenceEquals(actualStr, expectedStr))
            {
                errorDesc = "Substring value of StringBuilder is not the value ";
                errorDesc += string.Format("{0} as expected: actual({1})", expectedStr, actualStr);
                errorDesc += string.Format("\nStart index: {0}, length: {1} characters", startIndex, length);

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
        const string c_TEST_DESC = "PosTest2: Random start index and length is zero";
        string errorDesc;

        StringBuilder sb;
        int startIndex;
        int length;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
            sb = new StringBuilder(str);
            startIndex = TestLibrary.Generator.GetInt32(-55) % str.Length;
            length = 0;
            string expectedStr = string.Empty;
            string actualStr = sb.ToString(startIndex, length);

            if (actualStr != expectedStr)
            {
                errorDesc = "Substring value of StringBuilder is not the value ";
                errorDesc += string.Format("string.Empty as expected: actual({0})", actualStr);
                errorDesc += string.Format("\nStart index: {0}, length: {1} characters", startIndex, length);

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

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "P003";
        const string c_TEST_DESC = "PosTest3: Random start index is 0 and length equals the whole string's";
        string errorDesc;

        StringBuilder sb;
        int startIndex;
        int length;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
            sb = new StringBuilder(str);
            startIndex = 0;
            length = str.Length;
            string expectedStr = str;
            string actualStr = sb.ToString(startIndex, length);

            if (actualStr != expectedStr || Object.ReferenceEquals(actualStr, expectedStr))
            {
                errorDesc = "Substring value of StringBuilder is not the value ";
                errorDesc += string.Format("string.Empty as expected: actual({0})", actualStr);
                errorDesc += string.Format("\nStart index: {0}, length: {1} characters", startIndex, length);

                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Negative tests
    //ArgumentOutOfRangeException
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "N001";
        const string c_TEST_DESC = "NegTest1: start index is less than zero.";
        string errorDesc;

        StringBuilder sb;
        int startIndex, length;

        string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
        sb = new StringBuilder(str);

        startIndex = -1 * TestLibrary.Generator.GetInt32(-55) - 1;
        length = TestLibrary.Generator.GetInt32(-55) % (str.Length + 1);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            sb.ToString(startIndex, length);

            errorDesc = "ArgumentOutOfRangeException is not thrown as expected.";
            errorDesc += string.Format("\nString value of StringBuilder is {0}", str);
            errorDesc += string.Format("\nStart index is {0}, length is {1}", startIndex, length);
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nString value of StringBuilder is {0}", str);
            errorDesc += string.Format("\nStart index is {0}, length is {1}", startIndex, length);
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "N002";
        const string c_TEST_DESC = "NegTest2: length is less than zero.";
        string errorDesc;

        StringBuilder sb;
        int startIndex, length;

        string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
        sb = new StringBuilder(str);

        startIndex = TestLibrary.Generator.GetInt32(-55) % str.Length;
        length = -1 * TestLibrary.Generator.GetInt32(-55) - 1;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            sb.ToString(startIndex, length);

            errorDesc = "ArgumentOutOfRangeException is not thrown as expected.";
            errorDesc += string.Format("\nString value of StringBuilder is {0}", str);
            errorDesc += string.Format("\nStart index is {0}, length is {1}", startIndex, length);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nString value of StringBuilder is {0}", str);
            errorDesc += string.Format("\nStart index is {0}, length is {1}", startIndex, length);
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "N003";
        const string c_TEST_DESC = "NegTest3: The sum of startIndex and length is greater than the length of the current instance. ";
        string errorDesc;

        StringBuilder sb;
        int startIndex, length;

        string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
        sb = new StringBuilder(str);

        startIndex = TestLibrary.Generator.GetInt32(-55);
        if(startIndex > str.Length)
        {
            length = TestLibrary.Generator.GetInt32(-55);
        }
        else
        {
            length = str.Length - startIndex + 1 +
                TestLibrary.Generator.GetInt32(-55) % (Int32.MaxValue - str.Length + startIndex);
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            sb.ToString(startIndex, length);

            errorDesc = "ArgumentOutOfRangeException is not thrown as expected.";
            errorDesc += string.Format("\nString value of StringBuilder is {0}", str);
            errorDesc += string.Format("\nStart index is {0}, length is {1}", startIndex, length);
            TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nString value of StringBuilder is {0}", str);
            errorDesc += string.Format("\nStart index is {0}, length is {1}", startIndex, length);
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}

