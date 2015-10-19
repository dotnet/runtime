using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using TestLibrary;

/// <summary>
/// UInt32.ToString(System.string,IFormatProvider provider)
/// </summary>
public class UInt32ToString4
{
    public static int Main()
    {
        UInt32ToString4 ui32ts4 = new UInt32ToString4();
        TestLibrary.TestFramework.BeginTestCase("UInt32ToString4");

        if (ui32ts4.RunTests())
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
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;
        retVal = PosTest10() && retVal;
        retVal = PosTest11() && retVal;
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
	//We deleted the NegTest2 case because neutral cultures are now supported.
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: UInt32.ToString(minValue, 'G', en-US)");

        try
        {
            UInt32 uintA = UInt32.MinValue;
            string str = "G";
            CultureInfo myculture = new CultureInfo("en-US");
            String strA = uintA.ToString(str, myculture);
            if (strA != GlobLocHelper.OSUInt32ToString(uintA, str, myculture))
            {
                TestLibrary.TestFramework.LogError("001", "Expected: "+ GlobLocHelper.OSUInt32ToString(uintA, str, myculture) +
                    ", actual: "+strA);
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: UInt32(minValue, 'F', en-US)");

        try
        {
            UInt32 uintA = UInt32.MinValue;
            string str = "F";
            CultureInfo myculture = new CultureInfo("en-US");
            String strA = uintA.ToString(str, myculture);
            if (strA != GlobLocHelper.OSUInt32ToString(uintA, str, myculture))
            {
                TestLibrary.TestFramework.LogError("003", "Expected: "+ GlobLocHelper.OSUInt32ToString(uintA, str, myculture) +
                    ", actual: "+strA);
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: UInt32(minValue, 'F', es-ES)");

        try
        {
            UInt32 uintA = UInt32.MinValue;
            string str = "F";
            CultureInfo myculture = new CultureInfo("es-ES");
            String strA = uintA.ToString(str, myculture);
            if (strA != GlobLocHelper.OSUInt32ToString(uintA, str, myculture))
            {
                TestLibrary.TestFramework.LogError("005", "Expected: "+ GlobLocHelper.OSUInt32ToString(uintA, str, myculture) +
                    ", actual: "+strA);
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: UInt32.ToString(maxValue, 'X', en-US)");

        try
        {
            UInt32 uintA = UInt32.MaxValue;
            string str = "X";
            CultureInfo myculture = new CultureInfo("en-US");
            String strA = uintA.ToString(str, myculture);
            if (strA != "FFFFFFFF")
            {
                TestLibrary.TestFramework.LogError("007", "Expected: FFFFFFFF, actual: "+strA);
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: UInt32(MaxValue, 'x', en-US)");

        try
        {
            UInt32 uintA = UInt32.MaxValue;
            string str = "x";
            CultureInfo myculture = new CultureInfo("en-US");
            String strA = uintA.ToString(str, myculture);
            if (strA != "ffffffff")
            {
                TestLibrary.TestFramework.LogError("009", "Expected: ffffffff, actual: "+strA);
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
    public bool PosTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest6: UInt32(MaxValue, 'E', en-US)");

        try
        {
            UInt32 uintA = UInt32.MaxValue;
            string str = "E";
            CultureInfo myculture = new CultureInfo("en-US");
            String strA = uintA.ToString(str, myculture);
            if (strA != GlobLocHelper.OSUInt32ToString(uintA, str, myculture))
            {
                TestLibrary.TestFramework.LogError("011", "Expected: "+ GlobLocHelper.OSUInt32ToString(uintA, str, myculture) +
                    ", actual: "+strA);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest7()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest7: UInt32(MaxValue, 'E10', fr-FR)");

        try
        {
            UInt32 uintA = UInt32.MaxValue;
            string str = "E10";
            CultureInfo myculture = new CultureInfo("fr-FR");
            String strA = uintA.ToString(str, myculture);
            if (strA != GlobLocHelper.OSUInt32ToString(uintA, str, myculture))
            {
                TestLibrary.TestFramework.LogError("013", "Expected: "+ GlobLocHelper.OSUInt32ToString(uintA, str, myculture) +
                    ", actual: "+strA);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest8()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest8: UInt32(MaxValue, 'N', en-US)");

        try
        {
            UInt32 uintA = UInt32.MaxValue;
            string str = "N";
            CultureInfo myculture = new CultureInfo("en-US");
            String strA = uintA.ToString(str, myculture);
            if (strA != GlobLocHelper.OSUInt32ToString(uintA, str, myculture))
            {
                TestLibrary.TestFramework.LogError("015", "Expected: "+ GlobLocHelper.OSUInt32ToString(uintA, str, myculture) +
                    ", actual: "+strA);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest9()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest9: UInt32(MaxValue, 'N', sv-SE)");

        try
        {
            UInt32 uintA = UInt32.MaxValue;
            string str = "N";
            CultureInfo myculture = new CultureInfo("sv-SE");
            String strA = uintA.ToString(str, myculture);
            if (strA != GlobLocHelper.OSUInt32ToString(uintA, str, myculture))
            {
                TestLibrary.TestFramework.LogError("017", "Expected: "+ GlobLocHelper.OSUInt32ToString(uintA, str, myculture) +
                    ", actual: "+strA);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest10()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest10: UInt32.ToString(0x2c45e, 'x', en-US)");

        try
        {
            UInt32 uintA = 0x2c45e;
            string str = "x";
            CultureInfo myculture = new CultureInfo("en-US");
            String strA = uintA.ToString(str, myculture);
            if (strA != "2c45e")
            {
                TestLibrary.TestFramework.LogError("019", "Expected: 2c45e, actual: "+strA);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest11()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest11:UInt32.ToString(0x2c45e, 'X', en-US)");

        try
        {
            UInt32 uintA = 0x2c45e;
            string str = "X";
            CultureInfo myculture = new CultureInfo("en-US");
            String strA = uintA.ToString(str, myculture);
            if (strA != "2C45E")
            {
                TestLibrary.TestFramework.LogError("021", "Expected: 2C45E, actual: " + strA);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("022", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: FormatException thrown when the Format parameter is invalid");
        try
        {
            UInt32 uintA = (UInt32)(this.GetInt32(1, Int32.MaxValue) + this.GetInt32(0, Int32.MaxValue));
            String str = "Q";
            CultureInfo myculture = new CultureInfo("en-US");
            String strA = uintA.ToString(str, myculture);
            TestLibrary.TestFramework.LogError("N001.0", "Expected FormatException to be thrown");
            retVal = false;
        }
        catch (FormatException) { }
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

