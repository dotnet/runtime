// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

/// <summary>
/// System.Enum.IConvertibleToSingle(provider)
/// </summary>
public class EnumIConvertibleToSingle
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;


        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert zero to single");

        try
        {
            color c1 = color.blue;
            IConvertible i1 = c1 as IConvertible;
            float f1 = i1.ToSingle(null);
            if (f1 != 0.0)
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Test a normal enum of value 3 ");

        try
        {
            color c2 = color.white;
            IConvertible i2 = c2 as IConvertible;
            float f2 = i2.ToSingle(null);
            if (f2 != 3.0)
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert an int32 to single");

        try
        {
            e_test c2 = e_test.itemB;
            IConvertible i2 = c2 as IConvertible;
            float f2 = i2.ToSingle(null);
            if (f2 != Int32.MaxValue)
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Convert an int64.MinValue to single");

        try
        {
            e_test c2 = e_test.itemC;
            IConvertible i2 = c2 as IConvertible;
            float f2 = i2.ToSingle(null);
            if (f2 != Int64.MinValue)
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: Convert a negative to single");

        try
        {
            e_test c2 = e_test.itemA;
            IConvertible i2 = c2 as IConvertible;
            float f2 = i2.ToSingle(null);
            if (f2 != -4)
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

        TestLibrary.TestFramework.BeginScenario("PosTest6: Convert a negative zero to single");

        try
        {
            e_test c2 = e_test.itemD;
            IConvertible i2 = c2 as IConvertible;
            float f2 = i2.ToSingle(null);
            if (f2 != 0)
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
        EnumIConvertibleToSingle test = new EnumIConvertibleToSingle();

        TestLibrary.TestFramework.BeginTestCase("EnumIConvertibleToSingle");

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
        blue = 0,
        white = 3,
        red,
    }
    enum e_test : long
    {
        itemA = -4,
        itemB = Int32.MaxValue,
        itemC = Int64.MinValue,
        itemD = -0,
    }
}
