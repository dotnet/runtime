// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Convert.ToSByte(double)
/// <summary>
public class ConvertToSByte5
{
    #region Public Methods
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert a random Double greater than 0.5 to sbyte");

        try
        {
            double i;
            do
            {
                i = TestLibrary.Generator.GetDouble(-55);
            }
            while (i < 0.5D);
            SByte sByte = Convert.ToSByte(i);
            if (sByte != 1)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected,i is:"+i);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert a random Double less than 0.5 to sbyte");

        try
        {
            double i;
            do
            {
                i = TestLibrary.Generator.GetDouble(-55);
            }
            while (i > 0.5D);
            SByte sByte = Convert.ToSByte(i);
            if (sByte != 0)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected,i is: "+i);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert zero to sbyte");

        try
        {
            double i = 0;
            SByte sByte = Convert.ToSByte(i);
            if (sByte != 0)
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

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Convert a double whose decimal part is equal to 0.5 exactly");

        try
        {
            double integral = (double)(this.GetSByte(-50, 0) * 2 + 1);
            double i = integral - 0.5d;
            SByte sByte = Convert.ToSByte(i);
            if (sByte != (integral - 1))
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected,SByte is:"+sByte+"i is: "+i);
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: Convert a double indicating SbyteMaxValue to sbyte");

        try
        {
            double i = (double)SByte.MaxValue;
            SByte sByte = Convert.ToSByte(i);
            if (sByte != sbyte.MaxValue)
            {
                TestLibrary.TestFramework.LogError("009", "The result is not the value as expected");
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

        TestLibrary.TestFramework.BeginScenario("PosTest6: Convert a double indicating SbyteMinValue to sbyte");

        try
        {
            double i = (double)SByte.MinValue;
            SByte sByte = Convert.ToSByte(i);
            if (sByte != sbyte.MinValue)
            {
                TestLibrary.TestFramework.LogError("011", "The result is not the value as expected");
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Value is greater than SByte.MaxValue");

        try
        {
            double i = 128.5d;
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: Value is less than SByte.MinValue");

        try
        {
            double i = -150d;
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
        ConvertToSByte5 test = new ConvertToSByte5();

        TestLibrary.TestFramework.BeginTestCase("ConvertToSByte5");

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

    private SByte GetSByte(sbyte minValue, sbyte maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return (minValue);
            }
            if (minValue < maxValue)
            {
                return (sbyte)(minValue + TestLibrary.Generator.GetInt64(-55) % (maxValue - minValue));
            }
        }
        catch
        {
            throw;
        }
        return minValue;
    }
}
