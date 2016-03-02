// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// ConvertToUInt16(String,IFormatProvider)
/// </summary>
public class ConvertToUInt6417
{
    public static int Main()
    {
        ConvertToUInt6417 convertToUInt6417 = new ConvertToUInt6417();
        TestLibrary.TestFramework.BeginTestCase("ConvertToUInt6417");
        if (convertToUInt6417.RunTests())
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
        retVal = PosTest6() && retVal;
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert to UInt64 from string 1");
        try
        {
            string strVal = UInt64.MaxValue.ToString();
            ulong ulongVal = Convert.ToUInt64(strVal, null);
            if (ulongVal != UInt64.MaxValue)
            {
                TestLibrary.TestFramework.LogError("001", "The ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert to UInt64 from string 2");
        try
        {
            string strVal = UInt64.MaxValue.ToString();
            CultureInfo myculture = new CultureInfo("en-us");
            IFormatProvider provider = myculture.NumberFormat;
            ulong ulongVal = Convert.ToUInt64(strVal, provider);
            if (ulongVal != UInt64.MaxValue)
            {
                TestLibrary.TestFramework.LogError("003", "The ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert to UInt64 from string 3");
        try
        {
            string strVal = UInt64.MinValue.ToString();
            ulong ulongVal = Convert.ToUInt64(strVal, null);
            if (ulongVal != UInt64.MinValue)
            {
                TestLibrary.TestFramework.LogError("005", "The ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: Convert to UInt64 from string 4");
        try
        {
            string strVal = "-" + UInt64.MinValue.ToString();
            ulong ulongVal = Convert.ToUInt64(strVal, null);
            if (ulongVal != UInt64.MinValue)
            {
                TestLibrary.TestFramework.LogError("007", "The ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5: Convert to UInt64 from string 5");
        try
        {
            ulong sourceVal = (UInt64)TestLibrary.Generator.GetInt64(-55);
            string strVal = "+" + sourceVal.ToString();
            ulong ulongVal = Convert.ToUInt64(strVal, null);
            if (ulongVal != sourceVal)
            {
                TestLibrary.TestFramework.LogError("009", "The ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest6: Convert to UInt64 from string 6");
        try
        {
            string strVal = null;
            ulong ulongVal = Convert.ToUInt64(strVal, null);
            if (ulongVal != 0)
            {
                TestLibrary.TestFramework.LogError("011", "The ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: the string represents a number less than MinValue");
        try
        {
            int intVal = this.GetInt32(1, Int32.MaxValue);
            string strVal = "-" + intVal.ToString();
            ulong ulongVal = Convert.ToUInt64(strVal, null);
            TestLibrary.TestFramework.LogError("N001", "the string represents a number less than MinValue but not throw exception");
            retVal = false;
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2: the string represents a number greater than MaxValue");
        try
        {
            string strVal = UInt64.MaxValue.ToString() + "1";
            ulong ulongVal = Convert.ToUInt64(strVal, null);
            TestLibrary.TestFramework.LogError("N003", "the string represents a number greater than MaxValue but not throw exception");
            retVal = false;
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest3: the string does not consist of an optional sign followed by a sequence of digits ");
        try
        {
            string strVal = "helloworld";
            ulong ulongVal = Convert.ToUInt64(strVal, null);
            TestLibrary.TestFramework.LogError("N005", "the string does not consist of an optional sign followed by a sequence of digits but not throw exception");
            retVal = false;
        }
        catch (FormatException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest4: the string is empty string");
        try
        {
            string strVal = string.Empty;
            ulong ulongVal = Convert.ToUInt64(strVal, null);
            TestLibrary.TestFramework.LogError("N007", "the string is empty string but not throw exception");
            retVal = false;
        }
        catch (FormatException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region HelpMethod
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
    #endregion
}
