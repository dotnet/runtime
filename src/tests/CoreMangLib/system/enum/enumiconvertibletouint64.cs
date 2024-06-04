// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

/// <summary>
/// System.Enum.IConvertibleToUint64(System.Type,IFormatProvider )
/// </summary>
public class EnumIConvertibleToUint64
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;


        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert an enum of zero to Uint64");

        try
        {
            color c1 = color.blue;
            IConvertible i1 = c1 as IConvertible;
            UInt64 u1 = i1.ToUInt64(null);
            if (u1 != 0)
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Test a system defined enum type");

        try
        {
            Enum e2 = System.StringComparison.OrdinalIgnoreCase;
            UInt64 l2 = (e2 as IConvertible).ToUInt64(null);
            if (l2 != 5)
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert an enum of Uint64.maxvalue to Uint64");

        try
        {
            color c1 = color.white;
            IConvertible i1 = c1 as IConvertible;
            UInt64 u1 = i1.ToUInt64(null);
            if (u1 != UInt64.MaxValue)
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Convert an enum of negative zero to Uint64");

        try
        {
            color c1 = color.red;
            IConvertible i1 = c1 as IConvertible;
            UInt64 u1 = i1.ToUInt64(null);
            if (u1 != 0)
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected");
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

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Convert an enum of negative value to Uint64");

        try
        {
            e_test e1 = e_test.itemA;
            IConvertible i1 = e1 as IConvertible;
            UInt64 u1 = i1.ToUInt64(null);
            TestLibrary.TestFramework.LogError("101", "The OverflowException was not thrown as expected");
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
    #endregion
    #endregion

    [Fact]
    public static int TestEntryPoint()
    {
        EnumIConvertibleToUint64 test = new EnumIConvertibleToUint64();

        TestLibrary.TestFramework.BeginTestCase("EnumIConvertibleToUint64");

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

    enum color : ulong
    {
        blue = 0,
        white = UInt64.MaxValue,
        red = -0,
    }
    enum e_test : long
    {
        itemA = -123,
        itemB = Int64.MaxValue,
        itemC = Int64.MaxValue,
        itemD = -0,
    }
}
