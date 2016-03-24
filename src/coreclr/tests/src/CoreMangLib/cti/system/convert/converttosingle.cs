// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ConvertToSingle
{
    public static int Main()
    {
        ConvertToSingle ac = new ConvertToSingle();

        TestLibrary.TestFramework.BeginTestCase("Convert.ToSingle(...)");

        if (ac.RunTests())
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

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3()  && retVal;
        retVal = PosTest4()  && retVal;
        retVal = PosTest5()  && retVal;
        retVal = PosTest6()  && retVal;
        retVal = PosTest7()  && retVal;
        retVal = PosTest8()  && retVal;
        retVal = PosTest9()  && retVal;
        retVal = PosTest10()  && retVal;

        TestLibrary.TestFramework.LogInformation("");
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1()  && retVal;

        return retVal;
    }

    public bool PosTest1() { return PosTest<object>(1, (object)Single.MaxValue, Single.MaxValue); }
    public bool PosTest2() { return PosTest<object>(2, (object)Single.MinValue, Single.MinValue); }
    public bool PosTest3() { return PosTest<object>(3, null, 0); }

    public bool PosTest4() { return PosTest<Boolean>(4, true, 1f); }
    public bool PosTest5() { return PosTest<Boolean>(5, false, 0f); }

    public bool PosTest6() { return PosTest2(6, null, 0f); }
    public bool PosTest7() { return PosTest2(7, "28098" + System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + "23", 28098.23f); }

    public bool PosTest8() { return PosTest<object>(8, (object)Single.PositiveInfinity, Single.PositiveInfinity); }
    public bool PosTest9() { return PosTest<object>(9, (object)Single.NegativeInfinity, Single.NegativeInfinity); }
    public bool PosTest10() { return PosTest<object>(10, (object)Single.NaN, Single.NaN); }

    public bool NegTest1() { return NegTest<char>(1, ' ', typeof(System.InvalidCastException)); }

    public bool PosTest<T>(int id, T curValue, Single expValue)
    {
        bool            retVal = true;
        Single         newValue;
        IFormatProvider myfp;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ToSingle(...) (curValue:"+typeof(T)+" " +curValue+" newType:"+expValue.GetType()+")");

        try
        {
            newValue = Convert.ToSingle(curValue);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("000", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                retVal = false;
            }

            myfp     = null;
            newValue = Convert.ToSingle(curValue, myfp);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("001", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
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

    public bool PosTest2(int id, String curValue, Single expValue)
    {
        bool            retVal = true;
        Single         newValue;
        IFormatProvider myfp;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ToSingle(...) (curValue:string " +curValue+" newType:"+expValue.GetType()+")");

        try
        {
            newValue = Convert.ToSingle(curValue);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("003", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                retVal = false;
            }

            myfp     = null;
            newValue = Convert.ToSingle(curValue, myfp);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("004", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest<T>(int id, T curValue, Type exception)
    {
        bool            retVal = true;
        Single         newValue;

        TestLibrary.TestFramework.BeginScenario("NegTest"+id+": Convert.ToSingle(...) (curValue:string " +curValue+")");

        try
        {
            newValue = Convert.ToSingle(curValue);

            TestLibrary.TestFramework.LogError("006", "Exception expected");
            retVal = false;
        }
        catch (Exception e)
        {
            if (e.GetType() != exception)
            {
                TestLibrary.TestFramework.LogError("007", "Unexpected exception: " + e);
                retVal = false;
            }
        }

        return retVal;
    }
}

