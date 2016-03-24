// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// System.Globalization.StringInfo.GetNextTestElement(string,int32)
/// </summary>
public class StringInfoGetNextTextElement2
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
        retVal = PosTest4() && retVal;

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Get the text element from a random index in a string");

        try
        {
            string str = TestLibrary.Generator.GetString(-55, true, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            int index = this.GetInt32(8, str.Length);
            string result = StringInfo.GetNextTextElement(str, index);
            if (result != str[index].ToString())
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,the result is: " + result);
                retVal = false;
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: The first element is a surrogate pair");

        try
        {
            string str = "\uDBFF\uDFFF";
            string result = StringInfo.GetNextTextElement("ef45-;\uDBFF\uDFFFabcde", 6);
            if (result.Length != 2)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
                retVal = false;
            }
            if (result != str)
            {
                TestLibrary.TestFramework.LogError("004", "The result is not the value as expected,the result is: " + result);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: The element is  a combining character");

        try
        {
            string str = "a\u20D1";
            string result = StringInfo.GetNextTextElement("13229^a\u20D1abcde", 6);
            if (result != str)
            {
                TestLibrary.TestFramework.LogError("006", "The result is not the value as expected,the result is: " + result);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: The element is a combination of base character and several combining characters");

        try
        {
            string str = "z\uFE22\u20D1\u20EB";
            string result = StringInfo.GetNextTextElement("az\uFE22\u20D1\u20EBabcde", 1);
            if (result.Length != 4)
            {
                TestLibrary.TestFramework.LogError("008", "The result is not the value as expected,length is: " + result.Length);
                retVal = false;
            }
            if (result != str)
            {
                TestLibrary.TestFramework.LogError("009", "The result is not the value as expected,the result is: " + result);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
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
            string result = StringInfo.GetNextTextElement(str, 0);
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
            string str = "abc";
            string result = StringInfo.GetNextTextElement(str, -4);
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
            string str = "df8%^dk";
            string result = StringInfo.GetNextTextElement(str, -1);
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
        StringInfoGetNextTextElement2 test = new StringInfoGetNextTextElement2();

        TestLibrary.TestFramework.BeginTestCase("StringInfoGetNextTextElement2");

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
