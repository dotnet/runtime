// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// String.System.IConvertible.ToChar()
/// This method supports the .NET Framework infrastructure and is not intended 
/// to be used directly from your code. 
/// Converts a non-empty string of length one to a Char object. 
/// </summary>
class IConvertibleToChar
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    public static int Main()
    {
        IConvertibleToChar iege = new IConvertibleToChar();

        TestLibrary.TestFramework.BeginTestCase("for method: String.System.IConvertible.ToChar(IFormatProvider)");
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
        //retVal = PosTest2() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive test scenarioes

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Random single char string";
        const string c_TEST_ID = "P001";

        string strSrc;
        IFormatProvider provider;
        char ch;
        bool expectedValue = true;
        bool actualValue = false;

        ch = TestLibrary.Generator.GetChar(-55);
        strSrc = ch.ToString();
        provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            actualValue = (ch == ((IConvertible)strSrc).ToChar(provider));
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

    #endregion // end for positive test scenarioes

    #region Negative test scenarios

    //FormatException
    public bool NegTest1() //bug
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest1: The length of String object is greater than 1.";
        const string c_TEST_ID = "N001";

        string strSrc;
        IFormatProvider provider;

        strSrc = TestLibrary.Generator.GetString(-55, false, 2, c_MAX_STRING_LEN);
        provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            ((IConvertible)strSrc).ToChar(provider);
            TestLibrary.TestFramework.LogError("009" + "TestId-" + c_TEST_ID, "FormatException is not thrown as expected" + GetDataString(strSrc, provider));
            retVal = false;
        }
        catch (FormatException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e + GetDataString(strSrc, provider));
            retVal = false;
        }

        return retVal;
    }

    //FormatException
    public bool NegTest2() //bug
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest2: The String object is string.Empty";
        const string c_TEST_ID = "N002";

        string strSrc;
        IFormatProvider provider;

        strSrc = string.Empty;
        provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            ((IConvertible)strSrc).ToChar(provider);
            TestLibrary.TestFramework.LogError("009" + "TestId-" + c_TEST_ID, "FormatException is not thrown as expected" + GetDataString(strSrc, provider));
            retVal = false;
        }
        catch (FormatException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e + GetDataString(strSrc, provider));
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
