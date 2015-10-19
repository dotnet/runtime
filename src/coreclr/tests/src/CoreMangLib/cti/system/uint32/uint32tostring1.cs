using System;
using System.Collections.Generic;
using System.Text;
using TestLibrary;

/// <summary>
/// UInt32.ToString(System.string)
/// </summary>
public class UInt32ToString1
{
    public static int Main()
    {
        UInt32ToString1 ui32ts1 = new UInt32ToString1();
        TestLibrary.TestFramework.BeginTestCase("UInt32ToString1");

        if (ui32ts1.RunTests())
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
            String strA = uintA.ToString();
            if (strA != GlobLocHelper.OSUInt32ToString(uintA))
            {
                TestLibrary.TestFramework.LogError("001", "the ActualResult is not the ExpectResult");
                TestLibrary.TestFramework.LogInformation("Expected: " + GlobLocHelper.OSUInt32ToString(uintA) + "\nActual: " + strA);
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: UInt32 MaxValue ToString");

        try
        {
            UInt32 uintA = UInt32.MaxValue;
            String strA = uintA.ToString();
            if (strA != GlobLocHelper.OSUInt32ToString(uintA))
            {
                TestLibrary.TestFramework.LogError("003", "the ActualResult is not the ExpectResult");
                TestLibrary.TestFramework.LogInformation("Expected: " + GlobLocHelper.OSUInt32ToString(uintA) + "\nActual: " + strA);
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Random UInt32 num ToString 1");

        try
        {
            UInt32 uintA = 2147483648;
            String strA = uintA.ToString();
            if (strA != GlobLocHelper.OSUInt32ToString(uintA))
            {
                TestLibrary.TestFramework.LogError("005", "the ActualResult is not the ExpectResult");
                TestLibrary.TestFramework.LogInformation("Expected: " + GlobLocHelper.OSUInt32ToString(uintA) + "\nActual: " + strA);
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
        TestLibrary.TestFramework.BeginScenario("PosTest4:Random UInt32 num ToString 2");

        try
        {
            UInt32 uintA = 00065536;
            String strA = uintA.ToString();
            if (strA != GlobLocHelper.OSUInt32ToString(uintA))
            {
                TestLibrary.TestFramework.LogError("007", "the ActualResult is not the ExpectResult");
                TestLibrary.TestFramework.LogInformation("Expected: " + GlobLocHelper.OSUInt32ToString(uintA) + "\nActual: " + strA);
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
