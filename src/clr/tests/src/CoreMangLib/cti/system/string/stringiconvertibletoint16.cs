// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// String.System.IConvertible.ToInt16(IFormatProvider provider)
/// This method supports the .NET Framework infrastructure and is 
/// not intended to be used directly from your code. 
/// Converts the value of the current String object to a 16-bit signed integer.  
/// </summary>
class IConvertibleToInt16
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    public static int Main()
    {
        IConvertibleToInt16 iege = new IConvertibleToInt16();

        TestLibrary.TestFramework.BeginTestCase("for method: String.System.IConvertible.ToInt16(IFormatProvider)");
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
        retVal = PosTest5() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }

    #region Positive test scenarioes

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Random numeric string";
        const string c_TEST_ID = "P001";

        string strSrc;
        IFormatProvider provider;
        Int16 i;
        bool expectedValue = true;
        bool actualValue = false;

        i = TestLibrary.Generator.GetInt16(-55);
        strSrc = i.ToString();
        provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            actualValue = (i == ((IConvertible)strSrc).ToInt16(provider));
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

        const string c_TEST_DESC = "PosTest2: Positive sign";
        const string c_TEST_ID = "P002";

        string strSrc;
        IFormatProvider provider;
        NumberFormatInfo ni = new NumberFormatInfo();
        Int16 i;
        bool expectedValue = true;
        bool actualValue = false;

        i = TestLibrary.Generator.GetInt16(-55);
        ni.PositiveSign = TestLibrary.Generator.GetString(-55, false,false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        strSrc = ni.PositiveSign + i.ToString();
        provider = (IFormatProvider)ni;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            actualValue = (i == ((IConvertible)strSrc).ToInt16(provider));
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

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest3: string is Int16.MaxValue";
        const string c_TEST_ID = "P003";

        string strSrc;
        IFormatProvider provider;
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = Int16.MaxValue.ToString();
        provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            actualValue = (Int16.MaxValue == ((IConvertible)strSrc).ToInt16(provider));
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

    public bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest4: string is Int16.MinValue";
        const string c_TEST_ID = "P004";

        string strSrc;
        IFormatProvider provider;
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = Int16.MinValue.ToString();
        provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            actualValue = (Int16.MinValue == ((IConvertible)strSrc).ToInt16(provider));
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

    public bool PosTest5() // new update 8-14-2006 Noter(v-yaduoj)
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest5: Negative sign";
        const string c_TEST_ID = "P005";

        string strSrc;
        IFormatProvider provider;
        NumberFormatInfo ni = new NumberFormatInfo();
        Int16 i;
        bool expectedValue = true;
        bool actualValue = false;

        i = TestLibrary.Generator.GetInt16(-55);
        ni.NegativeSign = TestLibrary.Generator.GetString(-55, false,false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        strSrc = ni.NegativeSign + i.ToString();
        provider = (IFormatProvider)ni;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            actualValue = ((-1*i) == ((IConvertible)strSrc).ToInt16(provider));
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, provider);
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, provider));
            retVal = false;
        }

        return retVal;
    }

    #endregion // end for positive test scenarioes

    #region Negative test scenarios

    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest1: The value of String object cannot be parsed";
        const string c_TEST_ID = "N001";

        string strSrc;
        IFormatProvider provider;

        strSrc = "p" + TestLibrary.Generator.GetString(-55, false, 9, c_MAX_STRING_LEN);
        provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            ((IConvertible)strSrc).ToInt16(provider);
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

    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest2: The value of String object is a number greater than MaxValue";
        const string c_TEST_ID = "N002";

        string strSrc;
        IFormatProvider provider;
        int i;

        // new update 8-14-2006 Noter(v-yaduoj)
        i = TestLibrary.Generator.GetInt16(-55) + Int16.MaxValue + 1;
        strSrc = i.ToString();
        provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            // new update 8-14-2006 Noter(v-yaduoj)
            ((IConvertible)strSrc).ToInt16(provider);
            TestLibrary.TestFramework.LogError("011" + "TestId-" + c_TEST_ID, "OverflowException is not thrown as expected" + GetDataString(strSrc, provider));
            retVal = false;
        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e + GetDataString(strSrc, provider));
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest3: The value of String object is a number less than MinValue";
        const string c_TEST_ID = "N003";

        string strSrc;
        IFormatProvider provider;
        int i;

        // new update 8-14-2006 Noter(v-yaduoj)
        i = -1 * (TestLibrary.Generator.GetInt32(-55) % Int16.MaxValue) - Int16.MaxValue - 1;
        strSrc = i.ToString();
        provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            ((IConvertible)strSrc).ToInt16(provider);
            TestLibrary.TestFramework.LogError("013" + "TestId-" + c_TEST_ID, "OverflowException is not thrown as expected" + GetDataString(strSrc, provider));
            retVal = false;
        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014" + "TestId-" + c_TEST_ID, "Unexpected exception:" + e + GetDataString(strSrc, provider));
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
