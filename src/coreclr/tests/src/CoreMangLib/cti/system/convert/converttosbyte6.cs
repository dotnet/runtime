// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Convert.ToSByte(int16)
/// </summary>
public class ConvertToSByte6
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
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert an random int16 number to sbyte");

        try
        {
            Int16 i = this.GetInt16(sbyte.MinValue, sbyte.MaxValue);
            SByte sByte = Convert.ToSByte(i);
            if (sByte != i)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,i is: " + i);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert an int16 indicating SByteMaxValue number to sbyte");

        try
        {
            Int16 i = (Int16)SByte.MaxValue;
            SByte sByte = Convert.ToSByte(i);
            if (sByte != SByte.MaxValue)
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert an int16 indicating SByteMinValue number to sbyte");

        try
        {
            Int16 i = (Int16)SByte.MinValue;
            SByte sByte = Convert.ToSByte(i);
            if (sByte != SByte.MinValue)
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
            Int16 i = this.GetInt16(128, Int16.MaxValue);
            SByte sByte = Convert.ToSByte(i);
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

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: The argument is less than SByte.MinValue");

        try
        {
            Int16 i = this.GetInt16(Int16.MinValue, -128);
            SByte sByte = Convert.ToSByte(i);
            TestLibrary.TestFramework.LogError("103", "The OverflowException was not thrown as expected");
            retVal = false;
        }
        catch (OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ConvertToSByte6 test = new ConvertToSByte6();

        TestLibrary.TestFramework.BeginTestCase("ConvertToSByte6");

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
    private Int16 GetInt16(Int32 minValue, Int32 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return (Int16)minValue;
            }
            if (minValue < maxValue)
            {
                return (Int16)(minValue + TestLibrary.Generator.GetInt32(-55) % (maxValue - minValue));
            }
        }
        catch
        {
            throw;
        }

        return (Int16)minValue;
    }
}
