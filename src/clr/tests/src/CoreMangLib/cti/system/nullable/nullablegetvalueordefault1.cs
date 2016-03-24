// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Nullable<T>.GetValueOrDefault()[v-juwa]
/// </summary>
public class NullableGetValueOrDefault1
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

        const string c_TEST_DESC = "PosTest1: Verify the Nullable object's HasValue is true";
        const string c_TEST_ID = "P001";

        int value = TestLibrary.Generator.GetInt32(-55);
        int? nullObj = new Nullable<int>(value);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (nullObj.GetValueOrDefault() != value)
            {
                string errorDesc = "value is not " + value + " as expected: Actual(" + nullObj.GetValueOrDefault() + ")";
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

        const string c_TEST_DESC = "PosTes2: Verify the Nullable object's HasValue is false";
        const string c_TEST_ID = "P002";

        int? nullObj = new Nullable<int>();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (nullObj.GetValueOrDefault() != 0)
            {
                string errorDesc = "value is not 0 as expected: Actual(" + nullObj.GetValueOrDefault() + ")";
                TestLibrary.TestFramework.LogError("003 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #endregion

    public static int Main()
    {
        NullableGetValueOrDefault1 test = new NullableGetValueOrDefault1();

        TestLibrary.TestFramework.BeginTestCase("For method :System.Nullable<T>.GetValueOrDefault()");

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
