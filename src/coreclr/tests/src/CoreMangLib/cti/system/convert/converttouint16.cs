// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ConvertToUInt16
{
    public static int Main()
    {
        ConvertToUInt16 ac = new ConvertToUInt16();

        TestLibrary.TestFramework.BeginTestCase("Convert.ToUInt16(...)");

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

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1()  && retVal;
        retVal = NegTest2()  && retVal;

        return retVal;
    }

    // public static UInt16 ToUInt16(object value)
    public bool PosTest1() { return PosTest<object>(1, (object)UInt16.MaxValue, UInt16.MaxValue); }
    public bool PosTest2() { return PosTest<object>(2, (object)UInt16.MinValue, UInt16.MinValue); }
    public bool PosTest3() { return PosTest<object>(3, null, 0); }

    public bool PosTest4() { return PosTest<Boolean>(4, true, 1); }
    public bool PosTest5() { return PosTest<Boolean>(5, false, 0); }

    public bool PosTest6() { return PosTest2(6, null, 0); }
    public bool PosTest7() { return PosTest2(7, "1034", 1034); }

    // public static byte ToByte(object value) {
    public bool PosTest8() { return PosTest2(8, null, 0); }
    public bool PosTest9() { return PosTest2(9, "34", 34); }

    public bool NegTest1() { return NegTest2(1, Int32.MaxValue.ToString(), 10, typeof(System.OverflowException)); }
    public bool NegTest2() { return NegTest2(2, Int32.MinValue.ToString(), 10, typeof(System.OverflowException)); }

    public bool PosTest<T>(int id, T curValue, UInt16 expValue)
    {
        bool            retVal = true;
        UInt16         newValue;
        IFormatProvider myfp;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ToUInt16(...) (curValue:"+typeof(T)+" " +curValue+" newType:"+expValue.GetType()+")");

        try
        {
            newValue = Convert.ToUInt16(curValue);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("000", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                retVal = false;
            }

            myfp     = null;
            newValue = Convert.ToUInt16(curValue, myfp);

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

    public bool PosTest2(int id, String curValue, UInt16 expValue)
    {
        bool            retVal = true;
        UInt16         newValue;
        IFormatProvider myfp;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ToUInt16(...) (curValue:string " +curValue+" newType:"+expValue.GetType()+")");

        try
        {
            newValue = Convert.ToUInt16(curValue);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("003", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                retVal = false;
            }

            myfp     = null;
            newValue = Convert.ToUInt16(curValue, myfp);

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

        TestLibrary.TestFramework.BeginScenario("NegTest"+id+": Convert.ToUInt16(...) (curValue:"+typeof(T)+" " +curValue+"");

        try
        {
            Convert.ToUInt16(curValue);

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

    public bool NegTest2(int id, string curValue, int formBase, Type exception)
    {
        bool            retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest"+id+": Convert.ToUInt16(...) (curValue:string " +curValue+"");

        try
        {
            Convert.ToUInt16(curValue, formBase);

            TestLibrary.TestFramework.LogError("008", "Exception expected");
            retVal = false;
        }
        catch (Exception e)
        {
            if (e.GetType() != exception)
            {
                TestLibrary.TestFramework.LogError("009", "Unexpected exception: " + e);
                retVal = false;
            }
        }

        return retVal;
    }
}
