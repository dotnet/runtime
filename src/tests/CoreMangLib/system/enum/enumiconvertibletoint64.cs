// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

/// <summary>
/// System.Enum.IConvertibleToInt64(System.IFormatProvider)
/// </summary>
public class EnumIConvertibleToInt64
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;


        TestLibrary.TestFramework.BeginScenario("PosTest1: Test a customized enum type");

        try
        {
            color e1 = color.blue;
            IConvertible i1 = e1 as IConvertible;
            long l1 = i1.ToInt64(null);
            if (l1 != 100)
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
            Enum e2 = System.StringComparison.CurrentCultureIgnoreCase;
            long l2 = (e2 as IConvertible).ToInt64(null);
            if (l2 != 1)
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert a enum to int64, the value of which is Int32.Maximal ");

        try
        {
            e_test e3 = e_test.itemA;
            long l3 = (e3 as IConvertible).ToInt64(null);
            if (l3 != Int32.MaxValue)
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


        TestLibrary.TestFramework.BeginScenario("PosTest4: Convert a enum to Int64, the value of which is Int64.minvalue");

        try
        {
            e_test e4 = e_test.itemB;
            IConvertible i4 = e4 as IConvertible;
            long l4 = i4.ToInt64(null);
            if (l4 != Int64.MinValue)
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

    public bool PosTest5()
    {
        bool retVal = true;


        TestLibrary.TestFramework.BeginScenario("PosTest5: Convert a enum to Int64, the value of which is Int64.MaxValue");

        try
        {
            e_test? e5 = e_test.itemC;
            IConvertible i5 = e5 as IConvertible;
            long l5 = i5.ToInt64(null);
            if (l5 != Int64.MaxValue)
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
    #endregion

    #region Nagetive Test Cases
    #endregion
    #endregion

    [Fact]
    public static int TestEntryPoint()
    {
        EnumIConvertibleToInt64 test = new EnumIConvertibleToInt64();

        TestLibrary.TestFramework.BeginTestCase("EnumIConvertibleToInt64");

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
    enum color
    {
        blue = 100,
        white,
        red,
    }
    enum e_test : long
    {
        itemA = Int32.MaxValue,
        itemB = Int64.MinValue,
        itemC = Int64.MaxValue,
    }
}
