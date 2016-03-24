// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// StringBuilder.Capacity Property 
/// Gets or sets the maximum number of characters that can be contained 
/// in the memory allocated by the current instance. 
/// </summary>
public class StringBuilderCapacity
{
    private const int c_MIN_STR_LEN = 1;
    private const int c_MAX_STR_LEN = 260;

    private const int c_MAX_CAPACITY = Int16.MaxValue;

    public static int Main()
    {
        StringBuilderCapacity testObj = new StringBuilderCapacity();

        TestLibrary.TestFramework.BeginTestCase("for property: StringBuilder.Capacity");
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

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: Get the capacity property";
        string errorDesc;

        StringBuilder sb;
        int actualCapacity, expectedCapacity;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
            expectedCapacity = str.Length
                + TestLibrary.Generator.GetInt32(-55) % (c_MAX_CAPACITY - str.Length + 1);

            sb = new StringBuilder(str, expectedCapacity);

            actualCapacity = sb.Capacity;

            if (actualCapacity != expectedCapacity)
            {
                errorDesc = "Capacity of current StringBuilder " + sb + " is not the value ";
                errorDesc += string.Format("{0} as expected: actual({1})", expectedCapacity, actualCapacity);
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
        const string c_TEST_DESC = "PosTest2: Set the capacity property";
        string errorDesc;

        StringBuilder sb;
        int actualCapacity, expectedCapacity;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
            sb = new StringBuilder(str);

            expectedCapacity = str.Length
                + TestLibrary.Generator.GetInt32(-55) % (c_MAX_CAPACITY - str.Length + 1);

            sb.Capacity = expectedCapacity;
            actualCapacity = sb.Capacity;

            if (actualCapacity != expectedCapacity)
            {
                errorDesc = "Capacity of current StringBuilder " + sb + " is not the value ";
                errorDesc += string.Format("{0} as expected: actual({1})", expectedCapacity, actualCapacity);
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
        const string c_TEST_DESC = "NegTest1: The value specified for a set operation is less than the current length of this instance.";
        string errorDesc;

        StringBuilder sb;
        int capacity, currentInstanceLength;

        string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
        sb = new StringBuilder(str);
        currentInstanceLength = str.Length;
        capacity = TestLibrary.Generator.GetInt32(-55) % currentInstanceLength;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            sb.Capacity = capacity;
            errorDesc = "ArgumentOutOfRangeException is not thrown as expected.";
            errorDesc += string.Format("\nString value of StringBuilder is {0}", str);
            errorDesc += string.Format("\nCurrent length of instance is {0}, capacity specified is {1}", 
                               currentInstanceLength, capacity);
            TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nString value of StringBuilder is {0}", str);
            errorDesc += string.Format("\nCurrent length of instance is {0}, capacity specified is {1}",
                               currentInstanceLength, capacity);
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "N002";
        const string c_TEST_DESC = "NegTest2: The value specified for a set operation is less than zero.";
        string errorDesc;

        StringBuilder sb;
        int capacity, currentInstanceLength;

        string str = TestLibrary.Generator.GetString(-55, false, c_MIN_STR_LEN, c_MAX_STR_LEN);
        sb = new StringBuilder(str);
        currentInstanceLength = str.Length;
        capacity = -1 * TestLibrary.Generator.GetInt32(-55) - 1;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            sb.Capacity = capacity;
            errorDesc = "ArgumentOutOfRangeException is not thrown as expected.";
            errorDesc += string.Format("\nString value of StringBuilder is {0}", str);
            errorDesc += string.Format("\nCurrent length of instance is {0}, capacity specified is {1}",
                               currentInstanceLength, capacity);
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nString value of StringBuilder is {0}", str);
            errorDesc += string.Format("\nCurrent length of instance is {0}, capacity spdified is {1}",
                               currentInstanceLength, capacity);
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "N003";
        const string c_TEST_DESC = "NegTest3: The value specified for a set operation is greater than the maximum capacity.";
        string errorDesc;

        StringBuilder sb;
        int capacity;

        int maxCapacity = TestLibrary.Generator.GetInt32(-55) % c_MAX_CAPACITY;
        sb = new StringBuilder(0, maxCapacity);

        capacity = maxCapacity + 1 + TestLibrary.Generator.GetInt32(-55) % (int.MaxValue - maxCapacity);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            sb.Capacity = capacity;
            errorDesc = "ArgumentOutOfRangeException is not thrown as expected.";
            errorDesc += string.Format("\nMaximum capacity of instance is {0}, capacity spdified is {1}",
                               maxCapacity, capacity);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nMaximum capacity of instance is {0}, capacity spdified is {1}",
                               maxCapacity, capacity);
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}

