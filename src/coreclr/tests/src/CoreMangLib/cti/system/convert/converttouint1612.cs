using System;
/// <summary>
/// Convert.ToUInt16(SByte)
/// </summary>
public class ConvertToUInt1612
{
    public static int Main()
    {
        ConvertToUInt1612 convertToUInt1612 = new ConvertToUInt1612();

        TestLibrary.TestFramework.BeginTestCase("ConvertToUInt1612");
        if (convertToUInt1612.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert to UInt16 from SByte 1");
        try
        {
            SByte sbyteVal = SByte.MaxValue;
            ushort ushortVal = Convert.ToUInt16(sbyteVal);
            if (ushortVal != (UInt16)(SByte.MaxValue))
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert to UInt16 from SByte 2");
        try
        {
            SByte sbyteVal = (SByte)(this.GetInt32(0, (int)(SByte.MaxValue)));
            ushort ushortVal = Convert.ToUInt16(sbyteVal);
            if (ushortVal != (UInt16)(sbyteVal))
            {
                TestLibrary.TestFramework.LogError("003", "the ExpectResult is not the ActualResult");
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
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: the sbyte is less than the UInt16 minValue");
        try
        {
            SByte sbyteVal = (SByte)((this.GetInt32(1, (int)(SByte.MaxValue) + 2)) * (-1));
            ushort ushortVal = Convert.ToUInt16(sbyteVal);
            TestLibrary.TestFramework.LogError("N001", "the sbyte is less than the UInt16 minValue but not throw exception");
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
