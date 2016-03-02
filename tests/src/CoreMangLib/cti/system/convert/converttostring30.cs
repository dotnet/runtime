// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Convert.ToString(uInt32)
/// </summary>
public class ConvertToString30
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert a random UInt32 to string");

        try
        {
            UInt32 i = this.GetUInt32(UInt32.MinValue, UInt32.MaxValue);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert UInt32MaxValue to string");

        try
        {
            UInt32 i = UInt32.MaxValue;
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert UInt32MinValue to string");

        try
        {
            UInt32 i = UInt32.MinValue;
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
        ConvertToString30 test = new ConvertToString30();

        TestLibrary.TestFramework.BeginTestCase("ConvertToString30");

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
    private UInt32 GetUInt32(UInt32 minValue, UInt32 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return (minValue);
            }
            if (minValue < maxValue)
            {
                return (UInt32)(minValue + TestLibrary.Generator.GetInt64(-55) % (maxValue - minValue));
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }
}
