// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// StringBuilder.Length Property 
/// Gets or sets the length of this instance. 
/// </summary>
public class StringBuilderLength
{
    private const int c_MIN_STR_LEN = 1;
    private const int c_MAX_STR_LEN = 260;

    private const int c_MAX_CAPACITY = Int16.MaxValue;

    public static int Main()
    {
        StringBuilderLength testObj = new StringBuilderLength();

        TestLibrary.TestFramework.BeginTestCase("for property: StringBuilder.Length");
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

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: Get the Length property";
        string errorDesc;

        StringBuilder sb;
        int actualLength, expectedLength;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
            sb = new StringBuilder(str);

            expectedLength = str.Length;
            actualLength = sb.Length;

            if (actualLength != expectedLength)
            {
                errorDesc = "Length of current StringBuilder " + sb + " is not the value ";
                errorDesc += string.Format("{0} as expected: actual({1})", expectedLength, actualLength);
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
        const string c_TEST_DESC = "PosTest2: Set the Length property";
        string errorDesc;

        StringBuilder sb;
        int actualLength, expectedLength;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
            sb = new StringBuilder(str);

            expectedLength = TestLibrary.Generator.GetInt32(-55) % c_MAX_STR_LEN + 1;
            sb.Length = expectedLength;
            actualLength = sb.Length;

            if (actualLength != expectedLength)
            {
                errorDesc = "Length of current StringBuilder " + sb + " is not the value ";
                errorDesc += string.Format("{0} as expected: actual({1})", expectedLength, actualLength);
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
    //ArgumentOutOfRangeException
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "N001";
        const string c_TEST_DESC = "NegTest1: The value specified for a set operation is less than zero.";
        string errorDesc;

        StringBuilder sb;
        int length;

        string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
        sb = new StringBuilder(str);
        length = -1 * TestLibrary.Generator.GetInt32(-55) - 1;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            sb.Length = length;
            errorDesc = "ArgumentOutOfRangeException is not thrown as expected.";
            errorDesc += string.Format("\nLength specified is {0}", length);
            TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nLength specified is {0}", length);
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "N002";
        const string c_TEST_DESC = "NegTest2: The value specified for a set operation is greater than MaxCapacity.";
        string errorDesc;

        StringBuilder sb;
        int length;

        sb = new StringBuilder(0, c_MAX_CAPACITY);

        length = c_MAX_CAPACITY + 1 + 
            TestLibrary.Generator.GetInt32(-55) % (int.MaxValue - c_MAX_CAPACITY);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            sb.Length = length;
            errorDesc = "ArgumentOutOfRangeException is not thrown as expected.";
            errorDesc += string.Format("\nLength specified is {0}", length);
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nLength specified is {0}", length);
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
