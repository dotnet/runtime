using System;
using System.Globalization;
/// <summary>
/// SByte.System.IConvertible.ToUInt32(IFormatProvider)
/// </summary>
public class SByteIConvertibleToUInt32
{
    public static int Main()
    {
        SByteIConvertibleToUInt32 sbyteIConToUInt32 = new SByteIConvertibleToUInt32();
        TestLibrary.TestFramework.BeginTestCase("SByteIConvertibleToUInt32");
        if (sbyteIConToUInt32.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest1:The sbyte MaxValue IConvertible To UInt32");
        try
        {
            sbyte sbyteVal = sbyte.MaxValue;
            IConvertible iConvert = (IConvertible)(sbyteVal);
            UInt32 UInt32Val = iConvert.ToUInt32(provider);
            if (UInt32Val != 127)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:The random sbyte IConvertible To UInt32 1");
        try
        {
            sbyte sbyteVal = (sbyte)(this.GetInt32(0, 128));
            IConvertible iConvert = (IConvertible)(sbyteVal);
            UInt32 UInt32Val = iConvert.ToUInt32(null);
            if (UInt32Val != (UInt32)(sbyteVal))
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
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest3:The random sbyte IConvertible To UInt32 2");
        try
        {
            sbyte sbyteVal = (sbyte)(this.GetInt32(0, 128));
            IConvertible iConvert = (IConvertible)(sbyteVal);
            UInt32 UInt32Val = iConvert.ToUInt32(provider);
            if (UInt32Val != (UInt32)(sbyteVal))
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
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("NegTest1:The sbyte which out of UInt32 range IConvertible To UInt32");
        try
        {
            sbyte sbyteVal = (sbyte)(this.GetInt32(1, 129) * (-1));
            IConvertible iConvert = (IConvertible)(sbyteVal);
            UInt32 UInt32Val = iConvert.ToUInt32(provider);
            TestLibrary.TestFramework.LogError("N001", "The sbyte which out of UInt32 range IConvertible To UInt32 but not throw exception");
            retVal = false;
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpected exception: " + e);
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
