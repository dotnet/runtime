// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Nullable<T>.GetHashCode()[v-juwa]
/// </summary>
public class NullableGetHashCode
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Verify the Nullable<T> type is int";
        const string c_TEST_ID = "P001";

        int value = TestLibrary.Generator.GetInt32(-55);
        int? nullObj = new Nullable<int>(value);
        int expectedValue = value.GetHashCode();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (nullObj.GetHashCode() != expectedValue)
            {
                string errorDesc = "HashCod is not "+expectedValue+" as expected: Actual("+nullObj.GetHashCode()+")";
                errorDesc += "\n value is " + value;
                TestLibrary.TestFramework.LogError("001 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001" + "TestID_" + c_TEST_ID, "Unexpected exception: " + e + "\n value is " + value);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2: Verify the Nullable<T> type is char";
        const string c_TEST_ID = "P002";

        char value = TestLibrary.Generator.GetChar(-55);
        char? nullObj = new Nullable<char>(value);
        int expectedValue = value.GetHashCode();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (nullObj.GetHashCode() != expectedValue)
            {
                string errorDesc = "HashCod is not " + expectedValue + " as expected: Actual(" + nullObj.GetHashCode() + ")";
                errorDesc += "\n value is " + value;
                TestLibrary.TestFramework.LogError("003 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004 ", "Unexpected exception: " + e + "\n value is " + value);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest3: Verify the Nullable<T> object's HasValue is false";
        const string c_TEST_ID = "P003";

        char? nullObj = new Nullable<char>();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (nullObj.GetHashCode() != 0)
            {
                string errorDesc = "HashCod is not 0 as expected: Actual(" + nullObj.GetHashCode() + ")";
                TestLibrary.TestFramework.LogError("005 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006 ", "Unexpected exception: " + e );
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #endregion

    public static int Main()
    {
        NullableGetHashCode test = new NullableGetHashCode();

        TestLibrary.TestFramework.BeginTestCase("For method:System.Nullable<T>.GetHashCode()");

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
