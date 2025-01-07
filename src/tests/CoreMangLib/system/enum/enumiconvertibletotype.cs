// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

/// <summary>
/// System.Enum.IConvertibleToType(System.Type,IFormatProvider )
/// </summary>
public class EnumIConvertibleToType
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;


        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert an enum to string ");

        try
        {
            color c1 = color.blue;
            IConvertible i1 = c1 as IConvertible;
            string s1 = i1.ToType(typeof(string), null) as string;
            if (s1 != "blue")
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

        TestLibrary.TestFramework.BeginScenario("PosTest2:  Convert an enum to byte");

        try
        {
            color c1 = color.white;
            IConvertible i1 = c1 as IConvertible;
            byte s1 = (byte)i1.ToType(typeof(byte), null);
            if (s1 != 101)
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert an enum of negative value to single");

        try
        {
            e_test e1 = e_test.itemA;
            IConvertible i1 = e1 as IConvertible;
            float s1 = (float)i1.ToType(typeof(float), null);
            if (s1 != -123456789.0f)
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Convert an enum of int64.MaxValue to Int64 ");

        try
        {
            e_test e1 = e_test.itemC;
            IConvertible i1 = e1 as IConvertible;
            long s1 = (long)i1.ToType(typeof(long), null);
            if (s1 != Int64.MaxValue)
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: Convert an enum of int32.MinValue to Int32");

        try
        {
            e_test e1 = e_test.itemB;
            IConvertible i1 = e1 as IConvertible;
            int s1 = (int)i1.ToType(typeof(int), null);
            if (s1 != Int32.MinValue)
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

        TestLibrary.TestFramework.BeginScenario("PosTest6:Set the first argument as type of double");

        try
        {
            e_test e1 = e_test.itemC;
            IConvertible i1 = e1 as IConvertible;
            double s1 = (double)i1.ToType(typeof(double), null);
            if (s1 != (double)Int64.MaxValue)
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: Convert an enum of int32 to int16");

        try
        {
            e_test e1 = e_test.itemB;
            IConvertible i1 = e1 as IConvertible;
            Int16 s1 = (Int16)i1.ToType(typeof(Int16), null);
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
        EnumIConvertibleToType test = new EnumIConvertibleToType();

        TestLibrary.TestFramework.BeginTestCase("EnumIConvertibleToType");

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
        itemA = -123456789,
        itemB = Int32.MinValue,
        itemC = Int64.MaxValue,
        itemD = -0,
        itemE = 1220,
    }
}
