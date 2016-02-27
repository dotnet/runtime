// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Nullable<T>.Ctor(T)[v-juwa]
/// </summary>
public class NullableCtor
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Verify the value parameter is int";
        const string c_TEST_ID = "P001";

        int value = TestLibrary.Generator.GetInt32(-55);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int? nullObj = new Nullable<int>(value);

            if (!nullObj.HasValue)
            {
                string errorDesc = "HasValue is true as expected: Actual(false)";
                errorDesc += "\n value parameter is "+value;
                TestLibrary.TestFramework.LogError("001 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (nullObj.Value != value)
            {
                string errorDesc = "Value is "+value+" as expected: Actual("+nullObj.Value+")";
                errorDesc += "\n value parameter is " + value;
                TestLibrary.TestFramework.LogError("002 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e+"\n value parameter is "+value);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: Verify the value parameter is char";
        const string c_TEST_ID = "P002";

        char value = TestLibrary.Generator.GetChar(-55);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            char? nullObj = new Nullable<char>(value);

            if (!nullObj.HasValue)
            {
                string errorDesc = "HasValue is true as expected: Actual(false)";
                errorDesc += "\n value parameter is " + value;
                TestLibrary.TestFramework.LogError("004 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }

            if (nullObj.Value != value)
            {
                string errorDesc = "Value is " + value + " as expected: Actual(" + nullObj.Value + ")";
                errorDesc += "\n value parameter is " + value;
                TestLibrary.TestFramework.LogError("005 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e + "\n value parameter is " + value);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #endregion

    public static int Main()
    {
        NullableCtor test = new NullableCtor();

        TestLibrary.TestFramework.BeginTestCase("For Method:System.Nullable<T>.Ctor(T)");

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
