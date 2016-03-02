// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Nullable<T>.Value[v-juwa]
/// </summary>
public class NullableValue
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Verify the Nullable Type is int";
        const string c_TEST_ID = "P001";

        int value = TestLibrary.Generator.GetInt32(-55);
        int? nullObj = new Nullable<int>(value);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (nullObj.Value != value)
            {
                string errorDesc = "value is not "+ value+" as expected: Actual("+nullObj.Value+")";
                errorDesc += "\n value is " + value;
                TestLibrary.TestFramework.LogError("001 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e + "\n value is " + value);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: Verify the Nullable Type  is char";
        const string c_TEST_ID = "P002";

        char value = TestLibrary.Generator.GetChar(-55);
        char? nullObj = new Nullable<char>(value);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (nullObj.Value != value)
            {
                string errorDesc = "value is not " + value + " as expected: Actual(" + nullObj.Value + ")";
                errorDesc += "\n value is " + value;
                TestLibrary.TestFramework.LogError("003 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e + "\n value is " + value);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Negative Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest1: the Nullable object's HasValue is true";
        const string c_TEST_ID = "N001";

        char? nullObj = new Nullable<char>();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            char value = nullObj.Value;
            TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, "InvalidOperationException is not thrown ");
            retVal = false;
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        NullableValue test = new NullableValue();

        TestLibrary.TestFramework.BeginTestCase("For property:System.Nullable<T>.Value");

        if (test.RunTests())
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
}
