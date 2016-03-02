// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// String.GetEnumerator()
/// Retrieves an object that can iterate through the individual characters in this string.  
/// </summary>
class StringGetEnumerator
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    public static int Main()
    {
        StringGetEnumerator iege = new StringGetEnumerator();

        TestLibrary.TestFramework.BeginTestCase("for method: String.GetEnumerator()");
        if (iege.RunTests())
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

        return retVal;
    }

    #region Positive test scenarioes

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Random string";
        const string c_TEST_ID = "P001";

        string strSrc;
        IEnumerator<Char> iterator;
        bool condition = false;
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            iterator = ((IEnumerable<Char>)strSrc).GetEnumerator();

            condition = true;
            int index = 0;
            while (iterator.MoveNext())
            {
                condition = object.Equals(iterator.Current, strSrc[index]) && condition;
                index++;
            }
            iterator.Reset();
            
            actualValue = condition && (null != iterator);
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: string.Empty ";
        const string c_TEST_ID = "P002";

        string strSrc;
        IEnumerator<Char> iterator;
        bool condition = false;
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = string.Empty;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            iterator = ((IEnumerable<Char>)strSrc).GetEnumerator();

            condition = true;
            int index = 0;
            while (iterator.MoveNext())
            {
                condition = object.Equals(iterator.Current, strSrc[index]) && condition;
                index++;
            }
            iterator.Reset();

            actualValue = condition && (null != iterator);
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = @"PosTest3: string is \0 ";
        const string c_TEST_ID = "P003";

        string strSrc;
        IEnumerator<Char> iterator;
        bool condition = false;
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = "\0";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            iterator = ((IEnumerable<Char>)strSrc).GetEnumerator();

            condition = true;
            int index = 0;
            while (iterator.MoveNext())
            {
                condition = object.Equals(iterator.Current, strSrc[index]) && condition;
                index++;
            }
            iterator.Reset();

            actualValue = condition && (null != iterator);
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc);
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc));
            retVal = false;
        }

        return retVal;
    }

    #endregion // end for positive test scenarioes

    private string GetDataString(string strSrc)
    {
        string str1, str;
        int len1;

        if (null == strSrc)
        {
            str1 = "null";
            len1 = 0;
        }
        else
        {
            str1 = strSrc;
            len1 = strSrc.Length;
        }

        str = string.Format("\n[Source string value]\n \"{0}\"", str1);
        str += string.Format("\n[Length of source string]\n {0}", len1);

        return str;
    }

}
