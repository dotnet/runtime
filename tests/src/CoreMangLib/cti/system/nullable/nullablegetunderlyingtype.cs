// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Nullable.GetUnderlyingType(System.Type)[v-juwa]
/// </summary>
public class NullableGetUnderlyingType
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;


        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Verify the Type is Nullable<int>";
        const string c_TEST_ID = "P001";
        Type type = typeof(Nullable<int>);
        Type expectedType = typeof(int);


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Type result = Nullable.GetUnderlyingType(type);
            if (!Type.Equals(result,expectedType))
            {
                string errorDesc = "Return value is " + expectedType.ToString() + " as expected: Actual(" + result.ToString() + ")";
                errorDesc += " when type is " + type.ToString();
                TestLibrary.TestFramework.LogError("001 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e + "\n Type is Nullable<int>");
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: Verify the Type is int";
        const string c_TEST_ID = "P002";
        Type type = typeof(int);


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Type result = Nullable.GetUnderlyingType(type);
            if (result != null)
            {
                string errorDesc = "Return value is null as expected: Actual(" + result.ToString() + ")";
                errorDesc += " when type is " + type.ToString();
                TestLibrary.TestFramework.LogError("003 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e + "\n Type is int");
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

        const string c_TEST_DESC = "NegTest1: type is a null reference";
        const string c_TEST_ID = "N001";


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Nullable.GetUnderlyingType(null);
            TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, "ArgumentNullException is not thrown ");
            retVal = false;
        }
        catch (ArgumentNullException)
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
        NullableGetUnderlyingType test = new NullableGetUnderlyingType();

        TestLibrary.TestFramework.BeginTestCase("For Method:System.Nullable.GetUnderlyingType(System.Type)");

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
