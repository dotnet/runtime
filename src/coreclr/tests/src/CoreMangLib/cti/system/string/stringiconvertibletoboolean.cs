// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// String.System.IConvertible.ToBoolean(IFormatProvider provider)
/// This method supports the .NET Framework infrastructure and is not 
/// intended to be used directly from your code. 
/// Converts the value of the current String object to a Boolean value. 
/// </summary>
class IConvertibleToBoolean
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    public static int Main()
    {
        IConvertibleToBoolean iege = new IConvertibleToBoolean();

        TestLibrary.TestFramework.BeginTestCase("for method: String.System.IConvertible.ToBoolean(IFormatProvider)");
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
        retVal = PosTest4() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive test scenarioes

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: False string";
        const string c_TEST_ID = "P001";

        string strSrc;
        IFormatProvider provider;
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = "false";
        provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            actualValue = (false == ((IConvertible)strSrc).ToBoolean(provider));
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, provider);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, provider));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: True string";
        const string c_TEST_ID = "P002";

        string strSrc;
        IFormatProvider provider;
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = "true";
        provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            actualValue = (true == ((IConvertible)strSrc).ToBoolean(provider));
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, provider);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, provider));
            retVal = false;
        }

        return retVal;
    }

    //new update 8-14-2006 Noter(v-yaduoj)
    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest3: Trailing and leading white space";
        const string c_TEST_ID = "P003";

        string strSrc;
        IFormatProvider provider;
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = "  \n\r\ttrue \n\r\t ";
        provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            actualValue = (true == ((IConvertible)strSrc).ToBoolean(provider));
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, provider);
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, provider));
            retVal = false;
        }

        return retVal;
    }

    //new update 8-14-2006 Noter(v-yaduoj)
    public bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest4: Case insensitive";
        const string c_TEST_ID = "P004";

        string strSrc;
        IFormatProvider provider;
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = " TRue ";
        provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            actualValue = (true == ((IConvertible)strSrc).ToBoolean(provider));
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, provider);
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, provider));
            retVal = false;
        }

        return retVal;
    }

    #endregion // end for positive test scenarioes

    #region Negative test scenarios

    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest1: The value of String is not TrueString or FalseString.";
        const string c_TEST_ID = "N001";

        string strSrc;
        IFormatProvider provider;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            ((IConvertible)strSrc).ToBoolean(provider);
            TestLibrary.TestFramework.LogError("005" + "TestId-" + c_TEST_ID, "FormatException is not thrown as expected" + GetDataString(strSrc, provider));
            retVal = false;
        }
        catch (FormatException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e + GetDataString(strSrc, provider));
            retVal = false;
        }

        return retVal;
    }

    #endregion

    private string GetDataString(string strSrc, IFormatProvider provider)
    {
        string str1, str2, str;
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

        str2 = (null == provider) ? "null" : provider.ToString();

        str = string.Format("\n[Source string value]\n \"{0}\"", str1);
        str += string.Format("\n[Length of source string]\n {0}", len1);
        str += string.Format("\n[Format provider string]\n {0}", str2);

        return str;
    }

}
