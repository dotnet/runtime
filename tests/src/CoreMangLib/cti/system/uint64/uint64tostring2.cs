// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
using TestLibrary;

/// <summary>
/// UInt64.ToString(System.IFormatProvider)
/// </summary>
public class UInt64ToString1
{
    #region Public Methods
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
        if (Utilities.IsWindows)
        {
      //      retVal = NegTest1() && retVal;	// Disabled until neutral cultures are available
        }

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert UInt64MaxValue to string value and using cultureinfo \"en-US\"");

        try
        {
            UInt64 u64 = UInt64.MaxValue;
            CultureInfo cultureInfo = new CultureInfo("en-US");
            string str = u64.ToString(cultureInfo);
            if (str != "18446744073709551615")
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert UInt64MinValue to string value and using cultureinfo \"fr-FR\"");

        try
        {
            UInt64 u64 = UInt64.MinValue;
            CultureInfo cultureInfo = new CultureInfo("fr-FR");
            string str = u64.ToString(cultureInfo);
            if (str != "0")
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: A UInt64 number begin with zeros and using cultureinfo \"en-US\"");

        try
        {
            UInt64 u64 = 00009876;
            CultureInfo cultureInfo = new CultureInfo("en-US");
            string str = u64.ToString(cultureInfo);
            if (str != "9876")
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

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Convert a random uint64 to string value and using \"en-GB\"");

        try
        {
            UInt64 u64 = this.GetInt64(0, UInt64.MaxValue);
            CultureInfo cultureInfo = new CultureInfo("en-GB");
            string str = u64.ToString(cultureInfo);
            string str2 = Convert.ToString(u64);
            if (str != str2)
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: The provider is a null reference");

        try
        {
            UInt64 u64 = 000217639083000;
            CultureInfo cultureInfo = null;
            string str = u64.ToString(cultureInfo);
            if (str != "217639083000")
            {
                TestLibrary.TestFramework.LogError("009", "The result is not the value as expected");
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: The provider argument is invalid");

        try
        {
            UInt64 u64 = 1234500233;
            CultureInfo cultureInfo = new CultureInfo("pl");
            string str = u64.ToString(cultureInfo);
            TestLibrary.TestFramework.LogError("101", "The NotSupportedException is not thrown as expected");
            retVal = false;
        }
        catch (NotSupportedException)
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
        UInt64ToString1 test = new UInt64ToString1();

        TestLibrary.TestFramework.BeginTestCase("UInt64ToString1");

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
    #region ForTestObject
    private UInt64 GetInt64(UInt64 minValue, UInt64 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return minValue + (UInt64)TestLibrary.Generator.GetInt64(-55) % (maxValue - minValue);
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }
    #endregion
}
