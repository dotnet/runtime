// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Nullable<T>.Equals(Object)[v-juwa]
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
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Verify the Nullable object's value equals the parameter";
        const string c_TEST_ID = "P001";

        int value = TestLibrary.Generator.GetInt32(-55);
        int? nullObj = new Nullable<int>(value);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (!nullObj.Equals((object)value))
            {
                string errorDesc = "result is true as expected: Actual(false)";
                errorDesc += "\n parameter value is " + value;
                TestLibrary.TestFramework.LogError("001 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e+"parameter value is "+ value);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2: Verify the Nullable object's value doesn't equal the parameter";
        const string c_TEST_ID = "P002";

        Random rand = new Random(-55);
        int value = rand.Next(Int32.MinValue,Int32.MaxValue);
        int? nullObj = new Nullable<int>(value+1);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (nullObj.Equals((object)value))
            {
                string errorDesc = "result is false as expected: Actual(true)";
                errorDesc += "\n parameter value is " + value;
                errorDesc += "\n Nullable object's value is " + nullObj.Value;
                TestLibrary.TestFramework.LogError("003 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e + "\n parameter value is " + value+"\n Nullable object's value is "+nullObj.Value);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest3: Verify the Nullable's hasValue is false";
        const string c_TEST_ID = "P003";

        int value = TestLibrary.Generator.GetInt32(-55);
        int? nullObj = new Nullable<int>();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (nullObj.Equals((object)value))
            {
                string errorDesc = "result is false as expected: Actual(true)";
                errorDesc += "\n parameter value is " + value;
                errorDesc += "\n Nullable object's HasValue is false";
                TestLibrary.TestFramework.LogError("005 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e + "\n parameter value is " + value + "\n Nullable object's  HasValue is false");
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest4: Verify the Nullable's hasValue is true and parameter is null";
        const string c_TEST_ID = "P004";

        int value = TestLibrary.Generator.GetInt32(-55);
        int? nullObj = new Nullable<int>(value);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (nullObj.Equals(null))
            {
                string errorDesc = "result is false as expected: Actual(true)";
                errorDesc += "\n parameter is null";
                errorDesc += "\n Nullable object's value is "+nullObj.Value;
                TestLibrary.TestFramework.LogError("007 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e + "\n parameter value is " + value + "\n Nullable object's  HasValue is false");
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest5: Verify the Nullable's hasValue is false and parameter is null";
        const string c_TEST_ID = "P005";

        object value = null;
        int? nullObj = new Nullable<int>();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (!nullObj.Equals(value))
            {
                string errorDesc = "result is true as expected: Actual(fasle)";
                errorDesc += "\n parameter value is null";
                errorDesc += "\n Nullable object's HasValue is false";
                TestLibrary.TestFramework.LogError("009 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e + "\n parameter value is null  \n Nullable object's  HasValue is false");
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest6: Verify the Nullable's type is int and parameter is string";
        const string c_TEST_ID = "P006";

        object value = TestLibrary.Generator.GetString(-55, false,1,10);
        int i = TestLibrary.Generator.GetInt32(-55);
        int? nullObj = new Nullable<int>(i);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            if (nullObj.Equals(value))
            {
                string errorDesc = "result is true as expected: Actual(fasle)";
                errorDesc += "\n parameter value is "+value.ToString();
                errorDesc += "\n Nullable object's value is "+ i;
                TestLibrary.TestFramework.LogError("009 " + "TestID_" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010 " + "TestID_" + c_TEST_ID, "Unexpected exception: " + e + "\n parameter value is "+value.ToString()+" \n Nullable object's  value is "+i);
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

        TestLibrary.TestFramework.BeginTestCase("For method:System.Nullable<T>.Equals(Object)");

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
