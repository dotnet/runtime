// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// System.Globalization.StringInfo.GetTestElementEnumerator(string)
/// </summary>
public class StringInfoGetTextElementEnumerator1
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: The argument is a random string");

        try
        {
            string str = TestLibrary.Generator.GetString(-55, true, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator(str);
            int len = str.Length;
            TextElementEnumerator.MoveNext();
            for (int i = 0; i < len; i++)
            {
                if (TextElementEnumerator.Current.ToString() != str[i].ToString())
                {
                    TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,the str[i] is: " + str[i]);
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
            TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator("s\uDBFF\uDFFF$");
            TextElementEnumerator.MoveNext();
            if (TextElementEnumerator.Current.ToString() != "s")
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
                retVal = false;
            }
            TextElementEnumerator.MoveNext();
            if (TextElementEnumerator.Current.ToString() != str)
            {
                TestLibrary.TestFramework.LogError("004", "The result is not the value as expected,the current is: " + TextElementEnumerator.Current.ToString());
                retVal = false;
            }
            TextElementEnumerator.MoveNext();
            if (TextElementEnumerator.Current.ToString() != "$")
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
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
            TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator("13229^a\u20D1a");
            for (int i = 0; i < 7; i++)
            {
                TextElementEnumerator.MoveNext();
            }
            if (TextElementEnumerator.Current.ToString() != str)
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected,the current is: " + TextElementEnumerator.Current.ToString());
                retVal = false;
            }
            TextElementEnumerator.MoveNext();
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
            TextElementEnumerator TextElementEnumerator = StringInfo.GetTextElementEnumerator(str);
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
    #endregion
    #endregion

    public static int Main()
    {
        StringInfoGetTextElementEnumerator1 test = new StringInfoGetTextElementEnumerator1();

        TestLibrary.TestFramework.BeginTestCase("StringInfoGetTextElementEnumerator1");

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
