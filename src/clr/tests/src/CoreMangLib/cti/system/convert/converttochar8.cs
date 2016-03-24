// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Convert.ToChar(Object)
/// Converts the value of the specified Object to a Unicode character. 
/// </summary>
public class ConvertTochar
{
    public static int Main()
    {
        ConvertTochar testObj = new ConvertTochar();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToChar(Object)");
        if(testObj.RunTests())
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
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string errorDesc;

        object obj;
        char expectedValue;
        char actualValue;

        Int64 i = TestLibrary.Generator.GetInt64(-55) % (UInt16.MaxValue + 1);
        obj = i;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Object is Int64 value between 0 and UInt16.MaxValue.");
        try
        {
            actualValue = Convert.ToChar(obj);
            expectedValue = (char)i;

            if (actualValue != expectedValue)
            {
                errorDesc = string.Format("The character of Int64 value " + 
                                    obj + " is not the value \\u{0:x}" +
                                    " as expected: actual(\\u{1:x})", (int)expectedValue, (int)actualValue);
                TestLibrary.TestFramework.LogError("001", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe Int64 value is " + obj;
            TestLibrary.TestFramework.LogError("002", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string errorDesc;

        object obj;
        char expectedValue;
        char actualValue;

        byte b = TestLibrary.Generator.GetByte(-55);
        obj = b;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Object is byte value");
        try
        {
            actualValue = Convert.ToChar(obj);
            expectedValue = (char)b;

            if (actualValue != expectedValue)
            {
                errorDesc = string.Format("The character of byte value " +
                                    obj + " is not the value \\u{0:x}" +
                                    " as expected: actual(\\u{1:x})", (int)expectedValue, (int)actualValue);
                TestLibrary.TestFramework.LogError("003", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe byte value is " + obj;
            TestLibrary.TestFramework.LogError("004", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string errorDesc;

        object obj;
        char expectedValue;
        char actualValue;

        expectedValue = TestLibrary.Generator.GetChar(-55);
        obj = new string(expectedValue, 1);

        TestLibrary.TestFramework.BeginScenario("PosTest3: Object instance is string");
        try
        {
            actualValue = Convert.ToChar(obj);
            if (actualValue != expectedValue)
            {
                errorDesc = string.Format("The character of \"" + 
                                    obj + "\" is not the value \\u{0:x}" +
                                    " as expected: actual(\\u{1:x})", (int)expectedValue, (int)actualValue);
                TestLibrary.TestFramework.LogError("005", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe string is " + obj;
            TestLibrary.TestFramework.LogError("006", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        string errorDesc;

        object obj;
        char expectedValue;
        char actualValue;

        obj = TestLibrary.Generator.GetChar(-55);

        TestLibrary.TestFramework.BeginScenario("PosTest4: Object instance is character.");
        try
        {
            actualValue = Convert.ToChar(obj);
            expectedValue = (char)obj;

            if (actualValue != expectedValue)
            {
                errorDesc = string.Format("The character of \"" +
                    obj + "\" is not the value \\u{0:x}" +
                    " as expected: actual(\\u{1:x})", (int)expectedValue, (int)actualValue);
                TestLibrary.TestFramework.LogError("007", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe string is \"" + obj + "\"";
            TestLibrary.TestFramework.LogError("008", errorDesc);
            retVal = false;
        }
        return retVal;
    }
    #endregion

    #region Negative tests
    //OverflowException
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "N001";
        const string c_TEST_DESC = "NegTest1: Object instance is a negative Int32 value between Int32.MinValue and -1.";
        string errorDesc;

        Int32 i = -1 * TestLibrary.Generator.GetInt32(-55) - 1;
        object obj = i;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Convert.ToChar(obj);

            errorDesc = "OverflowException is not thrown as expected.";
            errorDesc += string.Format("\nThe Int32 value is {0}", i);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nThe Int32 value is {0}", i);
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "N002";
        const string c_TEST_DESC = "NegTest2: Object instance is a Int64 value between UInt16.MaxValue and Int64.MaxValue.";
        string errorDesc;

        Int64 i = TestLibrary.Generator.GetInt64(-55) % (Int64.MaxValue - UInt16.MaxValue) + 
                      UInt16.MaxValue + 1;
        object obj = i;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Convert.ToChar(obj);
            errorDesc = "OverflowException is not thrown as expected.";
            errorDesc += string.Format("\nThe Int64 value is {0}", i);
            TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nThe Int64 value is {0}", i);
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    //InvalidCastException
    public bool NegTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "N003";
        const string c_TEST_DESC = "NegTes3: Object instance does not implement the IConvertible interface. ";
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Convert.ToChar(new MyFoo());
            errorDesc = "InvalidCastException is not thrown as expected.";
            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (InvalidCastException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    //FormatException
    public bool NegTest4()
    {
        bool retVal = true;

        const string c_TEST_ID = "N004";
        const string c_TEST_DESC = "NegTest4: Object instance is a string whose length is longer than 1 characters.";
        string errorDesc;

        string str = TestLibrary.Generator.GetString(-55, false, 2, 256);
        object obj = str;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Convert.ToChar(obj);
            errorDesc = "FormatException is not thrown as expected.";
            errorDesc += "\nThe string is \"" + str + "\"";
            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (FormatException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += "\nThe string is \"" + str + "\"";
            TestLibrary.TestFramework.LogError("015" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    //ArgumentNullException
    public bool NegTest5()
    {
        bool retVal = true;

        const string c_TEST_ID = "N005";
        const string c_TEST_DESC = "NegTes5: Object instance is a null reference.";
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Convert.ToChar(null);
            errorDesc = "ArgumentNullException is not thrown as expected.";
            errorDesc += "\nThe string is <null>";
            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (ArgumentNullException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += "\nThe object is a null reference";
            TestLibrary.TestFramework.LogError("016" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    //InvalidCastException
    public bool NegTest6()
    {
        bool retVal = true;

        const string c_TEST_ID = "N006";
        const string c_TEST_DESC = "NegTes6: Object instance is double value.";
        string errorDesc;

        double d = TestLibrary.Generator.GetDouble(-55);
        object obj = d;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Convert.ToChar(obj);
            errorDesc = "InvalidCastException is not thrown as expected.";
            errorDesc += "\nThe string is \"" + obj.ToString() + "\"";
            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (InvalidCastException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += "\nThe object is a null reference";
            TestLibrary.TestFramework.LogError("016" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }
    #endregion

    #region Helper type
    //A class which does not implement the interface IConvertible
    internal class MyFoo
    {}
    #endregion
}
