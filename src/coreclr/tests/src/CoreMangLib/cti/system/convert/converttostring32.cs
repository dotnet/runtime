using System;

/// <summary>
/// System.Convert.ToString(uInt64)
/// </summary>
public class ConvertToString32
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert a random UInt64 to string");

        try
        {
            UInt64 i = this.GetUInt64(UInt64.MinValue, UInt64.MaxValue);
            string str = Convert.ToString(i);
            if (str != i.ToString())
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert UInt64MaxValue to string");

        try
        {
            UInt64 i = UInt64.MaxValue;
            string str = Convert.ToString(i);
            if (str != i.ToString())
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert UInt64MinValue to string");

        try
        {
            UInt64 i = UInt64.MinValue;
            string str = Convert.ToString(i);
            if (str != i.ToString())
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
    #endregion

    #region Nagetive Test Cases
    #endregion
    #endregion

    public static int Main()
    {
        ConvertToString32 test = new ConvertToString32();

        TestLibrary.TestFramework.BeginTestCase("ConvertToString32");

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
    private UInt64 GetUInt64(UInt64 minValue, UInt64 maxValue)
    {
        try
        {
            int i = this.GetInt32(0, 2);
            if (minValue == maxValue)
            {
                return (minValue);
            }
            if (minValue < maxValue)
            {
                return (UInt64)(minValue + (ulong)TestLibrary.Generator.GetInt64(-55) * 2 - (ulong)i);
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }
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
}
