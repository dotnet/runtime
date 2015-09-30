using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using TestLibrary;

/// <summary>
/// UInt32.ToString(System.IFormatProvider)
/// </summary>
public class UInt32ToString3
{
    public static int Main()
    {
        UInt32ToString3 ui32ts3 = new UInt32ToString3();
        TestLibrary.TestFramework.BeginTestCase("UInt32ToString3");

        if (ui32ts3.RunTests())
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
        if (Utilities.IsWindows)
        {
       //     retVal = NegTest1() && retVal; // Disabled until neutral cultures are available
        }
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: UInt32 minValue ToString");

        try
        {
            UInt32 uintA = UInt32.MinValue;
            CultureInfo myculture = new CultureInfo("en-us");
            String strA = uintA.ToString(myculture.NumberFormat);
            if (strA != GlobLocHelper.OSUInt32ToString(uintA, new CultureInfo("en-US")))
            {
                TestLibrary.TestFramework.LogError("001", "the ActualResult is not the ExpectResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: UInt32 maxValue ToString 1");

        try
        {
            UInt32 uintA = UInt32.MaxValue;
            CultureInfo myculture = new CultureInfo("en-us");
            String strA = uintA.ToString(myculture);
            if (strA != GlobLocHelper.OSUInt32ToString(uintA, new CultureInfo("en-US")))
            {
                TestLibrary.TestFramework.LogError("003", "the ActualResult is not the ExpectResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: UInt32 maxValue ToString 2");

        try
        {
            UInt32 uintA = UInt32.MaxValue;
            CultureInfo myculture = new CultureInfo("fr-FR");
            String strA = uintA.ToString(myculture);
            if (strA != GlobLocHelper.OSUInt32ToString(uintA, new CultureInfo("fr-FR")))
            {
                TestLibrary.TestFramework.LogError("005", "the ActualResult is not the ExpectResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: UInt32 maxValue ToString but provider is null");

        try
        {
            UInt32 uintA = UInt32.MaxValue;
            CultureInfo myculture = null;
            String strA = uintA.ToString(myculture);
            if (strA != GlobLocHelper.OSUInt32ToString(uintA, (CultureInfo)null))
            {
                TestLibrary.TestFramework.LogError("007", "the ActualResult is not the ExpectResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: random UInt32 ToString");

        try
        {
            UInt32 uintA = 2147483648;
            CultureInfo myculture = new CultureInfo("en-us");
            String strA = uintA.ToString(myculture);
            if (strA != GlobLocHelper.OSUInt32ToString(uintA, new CultureInfo("en-US")))
            {
                TestLibrary.TestFramework.LogError("009", "the ActualResult is not the ExpectResult");
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
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: The provider parameter is Invalid");
        try
        {
            UInt32 uintA = (UInt32)(this.GetInt32(1, Int32.MaxValue) + this.GetInt32(0, Int32.MaxValue));
            CultureInfo myculture = new CultureInfo("pl");
            String strA = uintA.ToString(myculture);
            retVal = false;
        }
        catch (NotSupportedException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N001", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region ForTestObject
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
