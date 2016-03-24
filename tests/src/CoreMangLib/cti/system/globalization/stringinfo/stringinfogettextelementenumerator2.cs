// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// System.Globalization.StringInfo.GetTestElementEnumerator(string,Int32)
/// </summary>
public class StringInfoGetTextElementEnumerator2
{
    private const int c_MINI_STRING_LENGTH = 8;
    private const int c_MAX_STRING_LENGTH = 256;

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: The argument is a random string,and start at a random index");

        try
        {
            string str = TestLibrary.Generator.GetString(-55, true, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            int len = str.Length;
            int index = this.GetInt32(8, len);
            TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator(str, index);
            TextElementEnumerator.MoveNext();
            for (int i = 0; i < len - index; i++)
            {
                if (TextElementEnumerator.Current.ToString() != str[i + index].ToString())
                {
                    TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,the str is: " + str[i + index]);
                    retVal = false;
                }
                TextElementEnumerator.MoveNext();
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: The string has a surrogate pair");

        try
        {
            string str = "\uDBFF\uDFFF";
            TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator("s\uDBFF\uDFFF$", 1);
            TextElementEnumerator.MoveNext();
            if (TextElementEnumerator.Current.ToString() != str)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected,the current is: " + TextElementEnumerator.Current.ToString());
                retVal = false;
            }
            TextElementEnumerator.MoveNext();
            if (TextElementEnumerator.Current.ToString() != "$")
            {
                TestLibrary.TestFramework.LogError("004", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: The string has a combining character");

        try
        {
            string str = "a\u20D1";
            TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator("13229^a\u20D1a", 6);
            TextElementEnumerator.MoveNext();
            if (TextElementEnumerator.Current.ToString() != str)
            {
                TestLibrary.TestFramework.LogError("006", "The result is not the value as expected,the current is: " + TextElementEnumerator.Current.ToString());
                retVal = false;
            }
            TextElementEnumerator.MoveNext();
            if (TextElementEnumerator.Current.ToString() != "a")
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected,the current is: " + TextElementEnumerator.Current.ToString());
                retVal = false;
            }
            if (TextElementEnumerator.MoveNext())
            {
                TestLibrary.TestFramework.LogError("008", "The result is not the value as expected,the current is: " + TextElementEnumerator.Current.ToString());
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The string is a null reference");

        try
        {
            string str = null;
            TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator(str, 0);
            TestLibrary.TestFramework.LogError("101", "The ArgumentNullException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: The index is out of the range of the string");

        try
        {
            string str = "dur8&p!";
            TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator(str, 10);
            TestLibrary.TestFramework.LogError("103", "The ArgumentOutOfRangeException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: The index is a negative number");

        try
        {
            string str = "dur8&p!";
            TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator(str, -10);
            TestLibrary.TestFramework.LogError("105", "The ArgumentOutOfRangeException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        StringInfoGetTextElementEnumerator2 test = new StringInfoGetTextElementEnumerator2();

        TestLibrary.TestFramework.BeginTestCase("StringInfoGetTextElementEnumerator2");

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
}
