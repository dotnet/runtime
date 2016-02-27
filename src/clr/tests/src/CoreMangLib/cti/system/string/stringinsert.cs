// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// String.Insert(Int32, String)  
/// Inserts a specified instance of String at a specified index position in this instance. 
/// </summary>
class StringInsert
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    private const int c_MAX_SHORT_STR_LEN = 31;//short string (<32 chars)
    private const int c_MIN_LONG_STR_LEN = 257;//long string ( >256 chars)
    private const int c_MAX_LONG_STR_LEN = 65535;

    public static int Main()
    {
        StringInsert si = new StringInsert();

        TestLibrary.TestFramework.BeginTestCase("for method: System.String.Insert(Int32, string)");
        if (si.RunTests())
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
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }

    #region Positive test scenarios

    public bool PosTest1() // new update 8-8-2006 Noter(v-yaduoj)
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Start index is 0";
        const string c_TEST_ID = "P001";

        int index;
        string strSrc, strInserting;
        bool condition1 = false; //Verify the inserting string
        bool condition2 = false; //Verify the source string
        bool expectedValue = true;
        bool actualValue = false;

        index = 0;
        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        //strSrc = "AABBB";
        strInserting = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        //strInserting = "%%";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string strNew = strSrc.Insert(index, strInserting);

            condition1 = (0 == string.CompareOrdinal(strInserting, strNew.Substring(index, strInserting.Length)));
            condition2 = (0 == string.CompareOrdinal(strSrc, strNew.Substring(0, index) + strNew.Substring(index + strInserting.Length)));
            actualValue = condition1 && condition2;
            if (expectedValue != actualValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("001" + "TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e);
            retVal = false;
        }

        return retVal;
    }

    // new update 8-8-2006 Noter(v-yaduoj)
    public bool PosTest2() 
    {
        bool retVal = true;

        const string c_TEST_DESC = "Start index equals the length of instance ";
        const string c_TEST_ID = "P002";

        int index;
        string strSrc, strInserting;
        bool condition1 = false; //Verify the inserting string
        bool condition2 = false; //Verify the source string
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        index = strSrc.Length;
        strInserting = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string strNew = strSrc.Insert(index, strInserting);

            condition1 = (0 == string.CompareOrdinal(strInserting, strNew.Substring(index, strInserting.Length)));
            condition2 = (0 == string.CompareOrdinal(strSrc, strNew.Substring(0, index) + strNew.Substring(index + strInserting.Length)));
            actualValue = condition1 && condition2;
            if (expectedValue != actualValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("003" + "TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e);
            retVal = false;
        }

        return retVal;
    }

    // new update 8-8-2006 Noter(v-yaduoj)
    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "Start index is a value between 1 and Instance.Length - 1 ";
        const string c_TEST_ID = "P003";

        int index;
        string strSrc, strInserting;
        bool condition1 = false; //Verify the inserting string
        bool condition2 = false; //Verify the source string
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        index = GetInt32(1, strSrc.Length - 1);
        strInserting = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string strNew = strSrc.Insert(index, strInserting);

            condition1 = (0 == string.CompareOrdinal(strInserting, strNew.Substring(index, strInserting.Length)));
            condition2 = (0 == string.CompareOrdinal(strSrc, strNew.Substring(0, index) + strNew.Substring(index + strInserting.Length)));
            actualValue = condition1 && condition2;
            if (expectedValue != actualValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("005" + "TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e);
            retVal = false;
        }

        return retVal;
    }

    // new update 8-8-2006 Noter(v-yaduoj)
    public bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_DESC = "String inserted is Sting.Empty";
        const string c_TEST_ID = "P004";

        int index;
        string strSrc, strInserting;
        bool condition1 = false; //Verify the inserting string
        bool condition2 = false; //Verify the source string
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        index = GetInt32(0, strSrc.Length);
        strInserting = String.Empty;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string strNew = strSrc.Insert(index, strInserting);

            condition1 = (0 == string.CompareOrdinal(strInserting, strNew.Substring(index, strInserting.Length)));
            condition2 = (0 == string.CompareOrdinal(strSrc, strNew.Substring(0, index) + strNew.Substring(index + strInserting.Length)));
            actualValue = condition1 && condition2;
            if (expectedValue != actualValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("007" + "TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e);
            retVal = false;
        }

        return retVal;
    }

    #endregion

    #region Negative test scenairos

    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "The string inserted is null";
        const string c_TEST_ID = "N001";

        int index;
        string strSource, str;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            strSource = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            index = GetInt32(0, strSource.Length);
            str = null;

            strSource.Insert(index, str);
            TestLibrary.TestFramework.LogError("009" + "TestId-" + c_TEST_ID, "ArgumentNullException is not thrown as expected");
            retVal = false;
        }
        catch (ArgumentNullException)
        {}
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "The start index is greater than the length of instance.";
        const string c_TEST_ID = "N002";

        int index;
        string strSource, str;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            strSource = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            index = GetInt32(strSource.Length + 1, Int32.MaxValue);
            str = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

            strSource.Insert(index, str);
            TestLibrary.TestFramework.LogError("011" + "TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "The start index is a negative integer";
        const string c_TEST_ID = "N003";

        int index;
        string strSource, str;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            strSource = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            index = -1 * GetInt32(0, Int32.MaxValue) - 1;
            str = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

            strSource.Insert(index, str);
            TestLibrary.TestFramework.LogError("013" + "TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e);
            retVal = false;
        }

        return retVal;
    }

    #endregion

    #region helper methods for generating test data

    private bool GetBoolean()
    {
        Int32 i = this.GetInt32(1, 2);
        return (i == 1) ? true : false;
    }

    //Get a non-negative integer between minValue and maxValue
    private Int32 GetInt32(Int32 minValue, Int32 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return minValue + TestLibrary.Generator.GetInt32(-55) % (maxValue - minValue);
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }

    private Int32 Min(Int32 i1, Int32 i2)
    {
        return (i1 <= i2) ? i1 : i2;
    }

    private Int32 Max(Int32 i1, Int32 i2)
    {
        return (i1 >= i2) ? i1 : i2;
    }

    #endregion
}

