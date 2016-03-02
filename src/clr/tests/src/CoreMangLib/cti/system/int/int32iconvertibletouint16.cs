// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

//System.Int32.System.IConvertible.ToUInt16(System.IFormatProvider)
public class Int32IConvertibleToUInt16
{
    public const int c_UINT16_MAXVALUE = 65535;

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

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert a random Int32 to UInt16");

        try
        {
            Int32 i1 = TestLibrary.Generator.GetInt16(-55);
            IConvertible Icon1 = (IConvertible)i1;
            if (Icon1.ToUInt16(null) != i1)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected.");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert zero to UInt16 ");

        try
        {
            Int32 i1 = 0;
            IConvertible Icon1 = (IConvertible)i1;
            if (Icon1.ToUInt16(null) != 0)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected.");
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

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest3: Check the boundary value ");

        try
        {
            Int32 i1 = c_UINT16_MAXVALUE;
            IConvertible Icon1 = (IConvertible)i1;
            if (Icon1.ToUInt16(null) != c_UINT16_MAXVALUE)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected.");
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: Test the overflowException ");

        try
        {
            Int32 i1 = 0;
            while (i1 <= c_UINT16_MAXVALUE)
            {
                i1 = TestLibrary.Generator.GetInt32(-55);
            }
            IConvertible Icon1 = (IConvertible)i1;
            ushort s1 = Icon1.ToUInt16(null);
            TestLibrary.TestFramework.LogError("101", "The overflow exception was not thrown as expected: ");
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: Test the negative int32 ");

        try
        {
            Int32 i1 = TestLibrary.Generator.GetInt16(-55);
            IConvertible Icon1 = (IConvertible)(-i1);
            ushort s1 = Icon1.ToUInt16(null);
            TestLibrary.TestFramework.LogError("103", "The overflow exception was not thrown as expected: ");
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
        Int32IConvertibleToUInt16 test = new Int32IConvertibleToUInt16();

        TestLibrary.TestFramework.BeginTestCase("Int32IConvertibleToUInt16");

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


