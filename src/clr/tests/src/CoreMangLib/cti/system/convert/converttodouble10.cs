// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// Convert.ToDouble(Object)
/// </summary>
public class ConvertToDouble10
{
    public static int Main()
    {
        ConvertToDouble10 testObj = new ConvertToDouble10();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToDouble(Object)");
        if (testObj.RunTests())
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
        retVal = NegTest1();
        retVal = NegTest2();
        retVal = NegTest3();
        retVal = NegTest4();

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest1: Verfify value is a vaild string and IFormatProvider is a null reference... ";
        string c_TEST_ID = "P001";

        string actualValue = "62356.123";
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            Double resValue = Convert.ToDouble((Object)actualValue, provider);
            if (Double.Parse(actualValue) != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest2: Verfify value is a valid double and IFormatProvider is a null reference... ";
        string c_TEST_ID = "P002";

        Double actualValue = TestLibrary.Generator.GetDouble(-55);
        IFormatProvider provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Double resValue = Convert.ToDouble((Object)actualValue,provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest3: Verfify value is a formated string... ";
        string c_TEST_ID = "P003";

        string actualValue = "365,982.123";
        NumberFormatInfo provider = new NumberFormatInfo();

        provider.NumberDecimalSeparator = ".";
        provider.NumberGroupSeparator = ",";
        provider.NumberGroupSizes = new int[] { 3 };

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Double resValue = Convert.ToDouble((Object)actualValue);
            if (Double.Parse(actualValue,provider) != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest4: Verfify value is a null reference and IFormatProvider is a null reference ... ";
        string c_TEST_ID = "P004";

        Object actualValue = null;
        IFormatProvider provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Double resValue = Convert.ToDouble(actualValue,provider);
            if (0 != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is 0";
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region NegativeTesting
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest1: value does not implement IConvertible interface and IFormatProvider is a null reference...";
        const string c_TEST_ID = "N001";


        Object actualValue = new Object();
        IFormatProvider provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Convert.ToDouble(actualValue,provider);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, "InvalidCastException is not thrown as expected.");
            retVal = false;
        }
        catch (InvalidCastException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest2: value is a string to double overflowed and IFormatProvider is a null reference...";
        const string c_TEST_ID = "N002";


        string actualValue = "2.7976931348623157E+308";
        IFormatProvider provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Convert.ToDouble((Object)actualValue, provider);
            TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, "OverflowException is not thrown as expected.");
            retVal = false;
        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest3: value is a string cantains invalid chars and IFormatProvider is a null reference...";
        const string c_TEST_ID = "N003";


        string actualValue = "3222.79asd";
        IFormatProvider provider = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Convert.ToDouble((Object)actualValue,provider);
            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, "FormatException is not thrown as expected.");
            retVal = false;
        }
        catch (FormatException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest4: value is a string that don't supply given IFormatProvider...";
        const string c_TEST_ID = "N004";


        string actualValue = "3,652,658.123";
        NumberFormatInfo provider = new NumberFormatInfo();

        provider.NumberDecimalSeparator = ",";
        provider.NumberGroupSeparator = ".";
        provider.NumberGroupSizes = new int[] { 3 };

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            Convert.ToDouble((Object)actualValue,provider);
            TestLibrary.TestFramework.LogError("015" + " TestId-" + c_TEST_ID, "FormatException is not thrown as expected.");
            retVal = false;
        }
        catch (FormatException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
