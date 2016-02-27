// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Convert.ToSByte(byte)
/// <summary>
public class ConvertToSByte2
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert a random byte value to sbyte");

        try
        {
            byte i = this.GetByte(0, (byte)sbyte.MaxValue);
            sbyte sByte = Convert.ToSByte(i);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert sbyteMinValue to sbyte");

        try
        {
            byte i = byte.MinValue;
            sbyte sByte = Convert.ToSByte(i);
            if (sByte != i)
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert a byte indicating sbyteMaxValue to sbyte");

        try
        {
            byte i = (byte)sbyte.MaxValue;
            sbyte sByte = Convert.ToSByte(i);
            if (sByte != i)
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
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The argument is greater than SByte.MaxValue");

        try
        {
            byte i = this.GetByte(sbyte.MaxValue + 1, byte.MaxValue);
            sbyte sByte = Convert.ToSByte(i);
            TestLibrary.TestFramework.LogError("101", "The OverflowException was not thrown as expected");
            retVal = false;
        }
        catch (OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ConvertToSByte2 test = new ConvertToSByte2();

        TestLibrary.TestFramework.BeginTestCase("ConvertToSByte2");

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

    private Byte GetByte(byte minValue, byte maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return (minValue);
            }
            if (minValue < maxValue)
            {
                return (byte)(minValue + TestLibrary.Generator.GetInt64(-55) % (maxValue - minValue));
            }
        }
        catch
        {
            throw;
        }
        return minValue;
    }
}
