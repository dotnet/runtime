using System;

/// <summary>
/// System.Convert.ToSByte(UInt16)
/// </summary>
public class ConvertToSByte11
{
    #region Public Methods
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

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert a random sbyte to sbyte");

        try
        {
            UInt16 i = this.GetUInt16(0, SByte.MaxValue);
            SByte sByte = Convert.ToSByte(i);
            if (sByte != i)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,i is:" + i);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert sbyteMaxValue to sbyte");

        try
        {
            UInt16 i = (UInt16)sbyte.MaxValue;
            SByte sByte = Convert.ToSByte(i);
            if (sByte != i)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected,i is:" + i);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert UInt16MinValue to SByte");

        try
        {
            UInt16 i = UInt16.MinValue;
            SByte sByte = Convert.ToSByte(i);
            if (sByte != i)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected,i is:" + i);
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

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Test the overflow exception");

        try
        {
            UInt16 i = this.GetUInt16(sbyte.MaxValue + 1, UInt16.MaxValue);
            SByte sByte = Convert.ToSByte(i);
            TestLibrary.TestFramework.LogError("101", "The OverflowException was not thrown as expected");
            retVal = false;
        }
        catch (OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ConvertToSByte11 test = new ConvertToSByte11();

        TestLibrary.TestFramework.BeginTestCase("ConvertToSByte11");

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
    private UInt16 GetUInt16(Int32 minValue, Int32 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return (UInt16)minValue;
            }
            if (minValue < maxValue)
            {
                return (UInt16)(minValue + TestLibrary.Generator.GetInt32(-55) % (maxValue - minValue));
            }
        }
        catch
        {
            throw;
        }

        return (UInt16)minValue;
    }
}
