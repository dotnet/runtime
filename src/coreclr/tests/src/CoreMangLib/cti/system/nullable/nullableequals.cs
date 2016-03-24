// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Nullable.Equals<T>(Nullable<T>,Nullable<T>)[v-juwa]
/// </summary>
public class NullableEquals
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Verify two Nullable<T> objects are null";
        const string c_TEST_ID = "P001";
        char? n1 = null;
        char? n2 = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (!Nullable.Equals<char>(n1, n2))
            {
                string errorDesc = "Return value is true as expected: Actual(false)";
                errorDesc += " when  n1 and n2 are null ";
                TestLibrary.TestFramework.LogError("001 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e + "\n n1 and n2 are null");
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: Verify n1 is null and n2 is not null";
        const string c_TEST_ID = "P002";
        char? n1 = null;
        char? n2 = TestLibrary.Generator.GetChar(-55);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (Nullable.Equals<char>(n1, n2))
            {
                string errorDesc = "Return value is false as expected: Actual(true)";
                errorDesc += " when  n1 is null and n2 is " + n2;
                TestLibrary.TestFramework.LogError("003 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e + "\n n1 is  null and n2 is " + n2);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest3: Verify n1 and n2 are eaual";
        const string c_TEST_ID = "P003";
        int? n1 = TestLibrary.Generator.GetInt32(-55);
        int? n2 = n1;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (!Nullable.Equals<int>(n1, n2))
            {
                string errorDesc = "Return value is true as expected: Actual(false)";
                errorDesc += " when  n1 is " + n1 + " and n2 is " + n2;
                TestLibrary.TestFramework.LogError("005 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e + "\n n1 is  " + n1 + " and n2 is " + n2);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest4: Verify n1 and n2 are not eaual";
        const string c_TEST_ID = "P004";
        Random rand = new Random(-55);
        int? n1 = rand.Next(Int32.MinValue, Int32.MaxValue);
        int? n2 = n1 + 1;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (Nullable.Equals<int>(n1, n2))
            {
                string errorDesc = "Return value is false as expected: Actual(true)";
                errorDesc += " when  n1 is " + n1 + " and n2 is " + n2;
                TestLibrary.TestFramework.LogError("007 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e + "\n n1 is  " + n1 + " and n2 is " + n2);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #endregion

    public static int Main()
    {
        NullableEquals test = new NullableEquals();

        TestLibrary.TestFramework.BeginTestCase("For method: System.Nullable.Equals<T>(Nullable<T>,Nullable<T>)");

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
