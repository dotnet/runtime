// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Nullable.Compare<T>(Nullable<T>,Nullable<T>)[v-juwa]
/// </summary>
public class NullableCompare
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
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;


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
        int expectedResult = 0;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int result = Nullable.Compare<char>(n1, n2);
            if (expectedResult != result)
            {
                string errorDesc = "Return value is not " + expectedResult + " as expected: Actual(" + result + ")";
                errorDesc += " when  n1 and n2 are null ";
                TestLibrary.TestFramework.LogError("001 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e+"\n n1 and n2 are null");
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: Verify n1 is null and n2 has value";
        const string c_TEST_ID = "P002";
        int? n1 = null;
        int? n2 = TestLibrary.Generator.GetInt32(-55);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int result = Nullable.Compare<int>(n1, n2);
            if (result>= 0)
            {
                string errorDesc = "Return value is less than zero as expected: Actual(" + result + ")";
                errorDesc += " \n  n1 is null ";
                errorDesc += "\n n2 is " + n2;
                TestLibrary.TestFramework.LogError("003 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e + "\n n1 is null and n2 is " + n2);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest3: Verify n1 has value and n2 is null";
        const string c_TEST_ID = "P003";
        int? n1 = TestLibrary.Generator.GetInt32(-55);
        int? n2 = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int result = Nullable.Compare<int>(n1, n2);
            if (result <= 0)
            {
                string errorDesc = "Return value is less than zero as expected: Actual(" + result + ")";
                errorDesc += " \n  n1 is "+ n1;
                errorDesc += "\n n2 is  null";
                TestLibrary.TestFramework.LogError("005 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e + "\n n1 is "+ n1+" and n2 is null");
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest4: Verify n1 and n2 are euqal";
        const string c_TEST_ID = "P004";
        char? n1 = TestLibrary.Generator.GetChar(-55);
        char? n2 = n1;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int result = Nullable.Compare<char>(n1, n2);
            if (result != 0)
            {
                string errorDesc = "Return value is not zero as expected: Actual(" + result + ")";
                errorDesc += " \n  n1 is " + n1;
                errorDesc += "\n n2 is  " + n2 ;
                TestLibrary.TestFramework.LogError("007 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e + "\n n1 is " + n1 + " and n2 is "+n2);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest5: Verify n1 less than n2";
        const string c_TEST_ID = "P005";
        Random rand = new Random(-55);
        int? n1 =rand.Next(Int32.MinValue,Int32.MaxValue) ;
        int? n2 = n1+1;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int result = Nullable.Compare<int>(n1, n2);
            if (result >= 0)
            {
                string errorDesc = "Return value is not less than zero as expected: Actual(" + result + ")";
                errorDesc += " \n  n1 is " + n1;
                errorDesc += "\n n2 is  " + n2;
                TestLibrary.TestFramework.LogError("009 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e + "\n n1 is " + n1 + " and n2 is " + n2);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest6: Verify n1 greater than n2";
        const string c_TEST_ID = "P006";
        Random rand = new Random(-55);
        int? n1 = rand.Next(Int32.MinValue+1, Int32.MaxValue);
        int? n2 = n1 - 1;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            int result = Nullable.Compare<int>(n1, n2);
            if (result <= 0)
            {
                string errorDesc = "Return value is not greater than zero as expected: Actual(" + result + ")";
                errorDesc += " \n  n1 is " + n1;
                errorDesc += "\n n2 is  " + n2;
                TestLibrary.TestFramework.LogError("011 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e + "\n n1 is " + n1 + " and n2 is " + n2);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #endregion

    public static int Main()
    {
        NullableCompare test = new NullableCompare();

        TestLibrary.TestFramework.BeginTestCase("Nullable.Compare<T>(Nullable<T>,Nullable<T>)");

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
