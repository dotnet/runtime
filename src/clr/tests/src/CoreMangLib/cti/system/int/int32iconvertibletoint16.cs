// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

//System.Int32.ConvertibleToInt16(System.IFormatProvider)
public class Int32IConvertibleToInt16
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert a Int32 to int16");

        try
        {
            int i1 = 0;
            while (i1 == 0)
            {
                i1 = (Int32)TestLibrary.Generator.GetInt16(-55);
            }
            IConvertible Icon1 = (IConvertible)(i1);
            if (Icon1.ToInt16(null) != i1)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert a negative int32 to int16");

        try
        {
            int i1 = 0;
            while (i1 == 0)
            {
                i1 = (Int32)TestLibrary.Generator.GetInt16(-55);
            }
            IConvertible Icon1 = (IConvertible)(-i1);
            if (Icon1.ToInt16(null) != (-i1))
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert zero to int16");

        try
        {
            int i1 = 0;
            IConvertible Icon1 = (IConvertible)(i1);
            if (Icon1.ToInt16(null) != i1)
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: Convert a Int32 which is bigger than int16 to Int16 ");

        try
        {
            Int32 i1 = 0;
            while (i1 <= 32767)
            {
                i1 = TestLibrary.Generator.GetInt32(-55);
            }
            IConvertible Icon1 = (IConvertible)(i1);
            if (Icon1.ToInt16(null) != i1)
            {
            }
            TestLibrary.TestFramework.LogError("101", "An overflowException was not thrown as expected ");
            retVal = false;
        }
        catch (OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Check the boundary value ");

        try
        {
            Int32 i1 = (Int32)(Int16.MinValue - 1);
            IConvertible Icon1 = (IConvertible)(i1);
            if (Icon1.ToInt16(null) != i1)
            {
            }
            TestLibrary.TestFramework.LogError("103", "An overflowException was not thrown as expected ");
            retVal = false;
        }
        catch (OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        Int32IConvertibleToInt16 test = new Int32IConvertibleToInt16();

        TestLibrary.TestFramework.BeginTestCase("Int32IConvertibleToInt16");

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
}
