// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

//system.Int16.System.IConvertibleToBoolean
public class Int16IConvertibleToBoolean
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: genarate a random int16 to boolean  ");

        try
        {
            Int16 i1 = 0;
            while (i1 == 0)
            {
                i1 = TestLibrary.Generator.GetInt16(-55);
            }
            IConvertible Icon1 = (IConvertible)(i1);
            if (Icon1.ToBoolean(null) != true)
            {
                TestLibrary.TestFramework.LogError("001", String.Format("The int16{0} to boolean is not true as expected ", i1));
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Zero to boolean  ");

        try
        {
            Int16 i1 = 0;
            IConvertible Icon1 = (IConvertible)(i1);
            if (Icon1.ToBoolean(null) != false)
            {
                TestLibrary.TestFramework.LogError("003", String.Format("The int16 {0} to boolean is not false as expected ", i1));
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: convert negative number to boolean  ");

        try
        {
            Int16 i1 = 0;
            while (i1 == 0)
            {
                i1 = TestLibrary.Generator.GetInt16(-55);
            }
            IConvertible Icon1 = (IConvertible)(-i1);
            if (Icon1.ToBoolean(null) != true)
            {
                TestLibrary.TestFramework.LogError("005", String.Format("The int16 {0} to boolean is not true as expected ", i1));
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
        Int16IConvertibleToBoolean test = new Int16IConvertibleToBoolean();

        TestLibrary.TestFramework.BeginTestCase("Int16IConvertibleToBoolean");

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
    private Int16 GetInt16(Int16 minValue, Int16 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return Convert.ToInt16(minValue + TestLibrary.Generator.GetInt16(-55) % (maxValue - minValue));
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }
}
