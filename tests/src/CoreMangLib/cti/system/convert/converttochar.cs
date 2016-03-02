// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ConvertToChar
{
    public static int Main()
    {
        ConvertToChar ac = new ConvertToChar();

        TestLibrary.TestFramework.BeginTestCase("Convert.ToChar(...)");

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

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1()  && retVal;
        retVal = NegTest2()  && retVal;
        retVal = NegTest3()  && retVal;
        retVal = NegTest4()  && retVal;
        retVal = NegTest5()  && retVal;
        retVal = NegTest6()  && retVal;

        return retVal;
    }

    // public static char ToChar(object value)
    public bool PosTest1() { return PosTest<object>(1, Char.MaxValue.ToString(), Char.MaxValue); }
    public bool PosTest2() { return PosTest<object>(2, Char.MinValue.ToString(), Char.MinValue); }
    public bool PosTest3() { return PosTest<object>(3, (object)null, (char)0); }

    // public static char ToChar(float value)
    public bool NegTest1() { return NegTest<float>(1, Single.MinValue, typeof(System.InvalidCastException)); }
    public bool NegTest2() { return NegTest<float>(2, Single.MaxValue, typeof(System.InvalidCastException)); }
    public bool NegTest3() { return NegTest<double>(3, Double.MinValue, typeof(System.InvalidCastException)); }
    public bool NegTest4() { return NegTest<double>(4, Double.MaxValue, typeof(System.InvalidCastException)); }
    public bool NegTest5() { return NegTest<decimal>(5, Decimal.MinValue, typeof(System.InvalidCastException)); }
    public bool NegTest6() { return NegTest<decimal>(6, Decimal.MaxValue, typeof(System.InvalidCastException)); }

    public bool PosTest<T>(int id, T curValue, char expValue)
    {
        bool            retVal = true;
        char            newValue;
        IFormatProvider myfp;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ToChar(...) (curValue:string " +curValue+" newType:"+expValue.GetType()+")");

        try
        {
            newValue = Convert.ToChar(curValue);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("000", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                retVal = false;
            }

            myfp     = null;
            newValue = Convert.ToChar(curValue, myfp);

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

    public bool NegTest<T>(int id, T curValue, Type exception)
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest"+id+": Convert.ToChar(...) (curValue:"+typeof(T)+" " +curValue+"");

        try
        {
            Convert.ToChar(curValue);

            TestLibrary.TestFramework.LogError("003", "Exception expected");
            retVal = false;
        }
        catch (Exception e)
        {
            if (e.GetType() != exception)
            {
                TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
                retVal = false;
            }
        }

        return retVal;
    }
}
