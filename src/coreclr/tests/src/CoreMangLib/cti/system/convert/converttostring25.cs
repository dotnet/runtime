// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// Convert.ToString(SByte,IFormatProvider)
/// </summary>
public class ConvertToString25
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert a random sbyte to string and the iformatprovider is null");

        try
        {
            sbyte sByte = this.GetSByte(sbyte.MinValue, sbyte.MaxValue);
            IFormatProvider iFormatProvider = null;
            string str = Convert.ToString(sByte, iFormatProvider);
            if (str != sByte.ToString())
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,sbyte is:" + sByte);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert sbyteMaxValue to string and the cultureinfo is \"en-US\"");

        try
        {
            sbyte sByte = SByte.MaxValue;
            IFormatProvider iFormatProvider = new CultureInfo("en-US");
            string str = Convert.ToString(sByte, iFormatProvider);
            if (str != sByte.ToString(iFormatProvider))
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert sbyteMinValue to string and the cultureinfo is \"fr-FR\"");

        try
        {
            sbyte sByte = SByte.MinValue;
            IFormatProvider iFormatProvider = new CultureInfo("fr-FR");
            string str = Convert.ToString(sByte, iFormatProvider);
            if (str != sByte.ToString(iFormatProvider))
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Using numberstyle to affect the string");

        try
        {
            sbyte sByte = -123;
            NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
            numberFormatInfo.NegativeSign = "$";
            string str = Convert.ToString(sByte, numberFormatInfo);
            if (str != "$123")
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
    #endregion

    #region Nagetive Test Cases

    #endregion
    #endregion

    public static int Main()
    {
        ConvertToString25 test = new ConvertToString25();

        TestLibrary.TestFramework.BeginTestCase("ConvertToString25");

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
    private SByte GetSByte(sbyte minValue, sbyte maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return (minValue);
            }
            if (minValue < maxValue)
            {
                return (sbyte)(minValue + TestLibrary.Generator.GetInt64(-55) % (maxValue - minValue));
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }
}
