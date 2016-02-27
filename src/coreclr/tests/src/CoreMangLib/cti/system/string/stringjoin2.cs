// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// String.Join(String, String[], Int32, Int32)
/// Concatenates a specified separator String between each 
/// element of a specified String array, yielding a single concatenated string. 
/// Parameters specify the first array element and number of elements to use. 
/// </summary>
class StringJoin2
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    private const int c_MAX_SHORT_STR_LEN = 31;//short string (<32 chars)
    private const int c_MIN_LONG_STR_LEN = 257;//long string ( >256 chars)
    private const int c_MAX_LONG_STR_LEN = 65535;

    private const int c_MIN_STR_ARRAY_LEN = 4;
    private const int c_MAX_STR_ARRAY_LEN = 127;

    private const int c_SUPER_MAX_INTEGER = 1 << 17;

    public static int Main()
    {
        StringJoin2 si = new StringJoin2();

        TestLibrary.TestFramework.BeginTestCase("for method: System.String.Join(String, String[], Int32, Int32)");
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
        retVal = PosTest5() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        //retVal = NegTest5() && retVal;

        return retVal;
    }

    #region Positive test scenarios

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Random separator and random string array, positive count";
        const string c_TEST_ID = "P001";

        string separator, joinedStr;
        int startIndex, count;
        bool condition1 = false; //Used to verify the element of the string array
        bool condition2 = false;  //used to verify the separator
        bool expectedValue, actualValue;
        int i, j, startIndex1, startIndex2;
        string[] strs;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        strs = TestLibrary.Generator.GetStrings(-55, false, c_MIN_STR_ARRAY_LEN, c_MAX_STR_ARRAY_LEN);
        //strs = new string[] { "AAAA", "BBBBB", "CCCCCC", "ddd", "EEEE"};
        separator = TestLibrary.Generator.GetString(-55, false, 1, c_MAX_STRING_LEN);
        //separator = "&&";
        startIndex = GetInt32(0, strs.GetLength(0) - 1);
        //startIndex = 1;
        count = GetInt32(1, strs.GetLength(0) - startIndex);
        //count = 1;

        try
        {
            joinedStr = string.Join(separator, strs, startIndex, count);

            string[] strsUsed = new string[count];
            for (int m = 0; m < count; m++)
            {
                strsUsed[m] = strs[startIndex + m];
            }

            i = GetInt32(0, strsUsed.GetLength(0) - 1);
            //i = 1;

            //Get source array element's start position of the joined string
            startIndex1 = 0;
            for (int m = i; m > 0;)
            {
                startIndex1 += separator.Length + strsUsed[--m].Length;
            }

            condition1 = (0 == String.CompareOrdinal(joinedStr.Substring(startIndex1, strsUsed[i].Length), strsUsed[i]));

            if (strsUsed.GetLength(0) > 1)
            {
                //Index of separator
                j = GetInt32(0, strsUsed.GetLength(0) - 2);
                startIndex2 = 0;
                while(j>0)
                {
                    startIndex2 += strsUsed[j--].Length + separator.Length;
                }
                startIndex2 += strsUsed[0].Length;
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
                errorDesc += GetDataString(strs, separator, startIndex, count);
                TestLibrary.TestFramework.LogError("001" + "TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e + GetDataString(strs, separator, startIndex, count));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: Random separator and random string array, count is zero";
        const string c_TEST_ID = "P002";

        string separator, joinedStr;
        int startIndex, count;
        bool expectedValue, actualValue;
        string[] strs;

        strs = TestLibrary.Generator.GetStrings(-55, false, c_MIN_STR_ARRAY_LEN, c_MAX_STR_ARRAY_LEN);
        separator = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        startIndex = GetInt32(0, strs.GetLength(0) - 1);
        count = 0;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            joinedStr = string.Join(separator, strs, startIndex, count);

            expectedValue = true;
            actualValue = (joinedStr == String.Empty);
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

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest3: Separator is null, random string array, positive count";
        const string c_TEST_ID = "P003";

        string separator, joinedStr;
        int startIndex, count;
        bool condition1 = false; //Used to verify the element of the string array
        bool condition2 = false;  //used to verify the separator
        bool expectedValue, actualValue;
        int i, j, startIndex1, startIndex2;
        string[] strs;

        strs = TestLibrary.Generator.GetStrings(-55, false, c_MIN_STR_ARRAY_LEN, c_MAX_STR_ARRAY_LEN);
        separator = null;
        startIndex = GetInt32(0, strs.GetLength(0) - 1);
        count = GetInt32(1, strs.GetLength(0) - startIndex);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            joinedStr = string.Join(separator, strs, startIndex, count);

            separator = string.Empty;
            string[] strsUsed = new string[count];
            for (int m = 0; m < count; m++)
            {
                strsUsed[m] = strs[startIndex + m];
            }

            i = GetInt32(0, strsUsed.GetLength(0) - 1);

            //Get source array element's start position of the joined string
            startIndex1 = 0;
            for (int m = i; m > 0; )
            {
                startIndex1 += separator.Length + strsUsed[--m].Length;
            }

            condition1 = (0 == String.CompareOrdinal(joinedStr.Substring(startIndex1, strsUsed[i].Length), strsUsed[i]));

            if (strsUsed.GetLength(0) > 1)
            {
                //Index of separator
                j = GetInt32(0, strsUsed.GetLength(0) - 2);
                startIndex2 = 0;
                while (j > 0)
                {
                    startIndex2 += strsUsed[j--].Length + separator.Length;
                }
                startIndex2 += strsUsed[0].Length;
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

    public bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest4: Start element is the last element of array, count of joined is 1";
        const string c_TEST_ID = "P004";

        string separator, joinedStr;
        int startIndex, count;
        bool condition1 = false; //Used to verify the element of the string array
        bool condition2 = false;  //used to verify the separator
        bool expectedValue, actualValue;
        int i, j, startIndex1, startIndex2;
        string[] strs;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        strs = TestLibrary.Generator.GetStrings(-55, false, c_MIN_STR_ARRAY_LEN, c_MAX_STR_ARRAY_LEN);
        //strs = new string[] { "AAAA", "BBBBB", "CCCCCC", "ddd", "EEEE"};
        separator = TestLibrary.Generator.GetString(-55, false, 1, c_MAX_STRING_LEN);
        //separator = "&&";
        startIndex = strs.GetLength(0) - 1;
        count = GetInt32(1, strs.GetLength(0) - startIndex);
        count = 1;

        try
        {
            joinedStr = string.Join(separator, strs, startIndex, count);

            string[] strsUsed = new string[count];
            for (int m = 0; m < count; m++)
            {
                strsUsed[m] = strs[startIndex + m];
            }

            i = GetInt32(0, strsUsed.GetLength(0) - 1);
            //i = 1;

            //Get source array element's start position of the joined string
            startIndex1 = 0;
            for (int m = i; m > 0; )
            {
                startIndex1 += separator.Length + strsUsed[--m].Length;
            }

            condition1 = (0 == String.CompareOrdinal(joinedStr.Substring(startIndex1, strsUsed[i].Length), strsUsed[i]));

            if (strsUsed.GetLength(0) > 1)
            {
                //Index of separator
                j = GetInt32(0, strsUsed.GetLength(0) - 2);
                startIndex2 = 0;
                while (j > 0)
                {
                    startIndex2 += strsUsed[j--].Length + separator.Length;
                }
                startIndex2 += strsUsed[0].Length;
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
                errorDesc += GetDataString(strs, separator, startIndex, count);
                TestLibrary.TestFramework.LogError("007" + "TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e + GetDataString(strs, separator, startIndex, count));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest5: Start element is the first element of array, join all elements of array";
        const string c_TEST_ID = "P005";

        string separator, joinedStr;
        int startIndex, count;
        bool condition1 = false; //Used to verify the element of the string array
        bool condition2 = false;  //used to verify the separator
        bool expectedValue, actualValue;
        int i, j, startIndex1, startIndex2;
        string[] strs;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        strs = TestLibrary.Generator.GetStrings(-55, false, c_MIN_STR_ARRAY_LEN, c_MAX_STR_ARRAY_LEN);
        separator = TestLibrary.Generator.GetString(-55, false, 1, c_MAX_STRING_LEN);
        startIndex =0;
        count =strs.GetLength(0);

        try
        {
            joinedStr = string.Join(separator, strs, startIndex, count);

            string[] strsUsed = new string[count];
            for (int m = 0; m < count; m++)
            {
                strsUsed[m] = strs[startIndex + m];
            }

            i = GetInt32(0, strsUsed.GetLength(0) - 1);
            //i = 1;

            //Get source array element's start position of the joined string
            startIndex1 = 0;
            for (int m = i; m > 0; )
            {
                startIndex1 += separator.Length + strsUsed[--m].Length;
            }

            condition1 = (0 == String.CompareOrdinal(joinedStr.Substring(startIndex1, strsUsed[i].Length), strsUsed[i]));

            if (strsUsed.GetLength(0) > 1)
            {
                //Index of separator
                j = GetInt32(0, strsUsed.GetLength(0) - 2);
                startIndex2 = 0;
                while (j > 0)
                {
                    startIndex2 += strsUsed[j--].Length + separator.Length;
                }
                startIndex2 += strsUsed[0].Length;
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
                errorDesc += GetDataString(strs, separator, startIndex, count);
                TestLibrary.TestFramework.LogError("009" + "TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e + GetDataString(strs, separator, startIndex, count));
            retVal = false;
        }

        return retVal;
    }

    #endregion

    #region Negative test scenairos

    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest1: The string array is a null reference";
        const string c_TEST_ID = "N001";

        string separator;
        string[] strs;
        int startIndex, count;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            separator = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            strs = null;
            startIndex = 0;
            count = 0;

            String.Join(separator, strs, startIndex, count);
            TestLibrary.TestFramework.LogError("011" + "TestId-" + c_TEST_ID, "ArgumentNullException is not thrown as expected");
            retVal = false;
        }
        catch (ArgumentNullException)
        {}
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest2: The start index of array is a negative value";
        const string c_TEST_ID = "N002";

        string separator;
        string[] strs;
        int startIndex, count;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            separator = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            strs = TestLibrary.Generator.GetStrings(-55, false, c_MIN_STR_ARRAY_LEN, c_MAX_STR_ARRAY_LEN);
            startIndex = -1 * TestLibrary.Generator.GetInt32(-55) - 1;
            count = 0;

            String.Join(separator, strs, startIndex, count);
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

    public bool NegTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest3: The count of array elements to join is a negative value";
        const string c_TEST_ID = "N003";

        string separator;
        string[] strs;
        int startIndex, count;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            separator = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            strs = TestLibrary.Generator.GetStrings(-55, false, c_MIN_STR_ARRAY_LEN, c_MAX_STR_ARRAY_LEN);
            startIndex = 0;
            count = -1 * TestLibrary.Generator.GetInt32(-55) - 1;

            String.Join(separator, strs, startIndex, count);
            TestLibrary.TestFramework.LogError("015" + "TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest4: The start index plus joined count is greater than numbers of elements in string array";
        const string c_TEST_ID = "N004";

        string separator;
        string[] strs;
        int startIndex, count;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            separator = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            strs = TestLibrary.Generator.GetStrings(-55, false, c_MIN_STR_ARRAY_LEN, c_MAX_STR_ARRAY_LEN);
            startIndex = TestLibrary.Generator.GetInt32(-55);
            count = strs.GetLength(0) - startIndex + GetInt32(1, Int32.MaxValue - strs.GetLength(0));

            String.Join(separator, strs, startIndex, count);
            TestLibrary.TestFramework.LogError("017" + "TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5() //bug
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest5: Out of memory";
        const string c_TEST_ID = "N005";

        string separator;
        string[] strs;
        int startIndex, count;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            separator = new string(TestLibrary.Generator.GetChar(-55), c_SUPER_MAX_INTEGER);
            strs = new string[c_SUPER_MAX_INTEGER];

            //for (int i = 0; i < strs.GetLength(0); i++)
            //{
            //    strs[i] = new string(TestLibrary.Generator.GetChar(-55), 1);
            //}
            
            startIndex = 0;
            count = strs.GetLength(0);

            string joinedStr = String.Join(separator, strs, startIndex, count);
            TestLibrary.TestFramework.LogError("019" + "TestId-" + c_TEST_ID, "OutOfMemoryException is not thrown as expected");
            retVal = false;
        }
        catch (OutOfMemoryException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e);
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

    private string GetDataString(string[] strs, string separator, int startIndex, int count)
    {
        string str1, str2, str;
        int len1, len2;
        str2 = string.Empty;

        if (null == separator)
        {
            str1 = "null";
            len1 = 0;
        }
        else
        {
            str1 = separator;
            len1 = separator.Length;
        }
        if (null == strs)
        {
            str2 = "null";
            len2 = 0;
        }
        else 
        {
            len2 = strs.GetLength(0);
            for (int i = 0; i < len2; i++)
            {
                str2 += "\n" + strs[i];
            }
        }

        str = string.Format("\n[String array value]\n \"{0}\"", str2);
        str += string.Format("\n[Separator string value]\n \"{0}\"", str1);
        str += string.Format("\n[Length of string array]\n \"{0}\"", len2);
        str += string.Format("\n[Length of separator string]\n \"{0}\"", len1);
        str += string.Format("\n[Joined elements start index]: {0}", startIndex);
        str += string.Format("\n[Joined elements count]: {0}", count);

        return str;
    }
}

