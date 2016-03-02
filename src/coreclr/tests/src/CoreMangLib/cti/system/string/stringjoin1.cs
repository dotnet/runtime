// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// String.Join(String, String[])  
/// Concatenates a specified separator String between each element 
///  of a specified String array, yielding a single concatenated string. 
/// </summary>
class StringJoin1
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    private const int c_MAX_SHORT_STR_LEN = 31;//short string (<32 chars)
    private const int c_MIN_LONG_STR_LEN = 257;//long string ( >256 chars)
    private const int c_MAX_LONG_STR_LEN = 65535;

    private const int c_MIN_STR_ARRAY_LEN = 4;
    private const int c_MAX_STR_ARRAY_LEN = 127;

    public static int Main()
    {
        StringJoin1 si = new StringJoin1();

        TestLibrary.TestFramework.BeginTestCase("for method: System.String.Join(String, String[])");
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
        /*retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;*/

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive test scenarios

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "Random separator and random string array";
        const string c_TEST_ID = "P001";

        string separator, joinedStr;
        bool condition1 = false; //Used to verify the element of the string array
        bool condition2 = false;  //used to verify the separator
        bool expectedValue, actualValue;
        int i, j, startIndex1, startIndex2;
        string[] strs;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            strs = TestLibrary.Generator.GetStrings(-55, false, c_MIN_STR_ARRAY_LEN, c_MAX_STR_ARRAY_LEN);
            //strs = new string[] { "AAAA", "BBBBB", "CCCCCC" };
            separator = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

            joinedStr = string.Join(separator, strs);
            i = GetInt32(0, strs.GetLength(0) - 1);
            //i = 1;

            //Get source array element's start position of the joined string
            startIndex1 = 0;
            for (int m = i; m > 0;)
            {
                startIndex1 += separator.Length + strs[--m].Length;
            }

            condition1 = (0 == String.CompareOrdinal(joinedStr.Substring(startIndex1, strs[i].Length), strs[i]));

            if(strs.GetLength(0) > 1)
            {
                //new update 8-8-2006 Noter(v-yaduoj)
                //Index of separator
                j = GetInt32(1, strs.GetLength(0) - 1);
                startIndex2 = 0;
                while(j>0)
                {
                    startIndex2 += strs[j--].Length + separator.Length;
                }
                startIndex2 += strs[0].Length;
                condition2 = (0 == String.CompareOrdinal(joinedStr.Substring(startIndex2, separator.Length), separator));
            }
            else
            {
                condition2 = true;
            }

            expectedValue = true;
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

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "Separator is null, random string array";
        const string c_TEST_ID = "P002";

        string separator, joinedStr;
        bool condition1 = false; //Used to verify the element of the string array
        bool condition2 = false;  //used to verify the separator
        bool expectedValue, actualValue;
        int i, j, startIndex1, startIndex2;
        string[] strs;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            strs = TestLibrary.Generator.GetStrings(-55, false, c_MIN_STR_ARRAY_LEN, c_MAX_STR_ARRAY_LEN);
            separator = null;

            joinedStr = string.Join(separator, strs);

            separator = string.Empty;
            i = GetInt32(0, strs.GetLength(0) - 1);

            //Get source array element's start position of the joined string
            startIndex1 = 0;
            for (int m = i; m > 0; )
            {
                startIndex1 += separator.Length + strs[--m].Length;
            }

            condition1 = (0 == String.CompareOrdinal(joinedStr.Substring(startIndex1, strs[i].Length), strs[i]));

            if (strs.GetLength(0) > 1)
            {
                //new update 8-8-2006 Noter(v-yaduoj)
                //Index of separator
                j = GetInt32(0, strs.GetLength(0) - 2); 
                startIndex2 = 0;
                while (j > 0)
                {
                    startIndex2 += strs[j--].Length + separator.Length;
                }
                startIndex2 += strs[0].Length;
                condition2 = (0 == String.CompareOrdinal(joinedStr.Substring(startIndex2, separator.Length), separator));
            }
            else
            {
                condition2 = true;
            }

            expectedValue = true;
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

    #endregion

    #region Negative test scenairos

    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "The string array is null";
        const string c_TEST_ID = "N001";

        string separator;
        string[] strs;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            separator = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            strs = null;

            String.Join(separator, strs);
            TestLibrary.TestFramework.LogError("005" + "TestId-" + c_TEST_ID, "ArgumentNullException is not thrown as expected");
            retVal = false;
        }
        catch (ArgumentNullException)
        {}
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("06" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e);
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

