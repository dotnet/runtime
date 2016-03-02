// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ConvertToUInt32
{
    public static int Main()
    {
        ConvertToUInt32 ac = new ConvertToUInt32();

        TestLibrary.TestFramework.BeginTestCase("Convert.ToUInt32(...)");

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

        return retVal;
    }

    public bool PosTest1() { return PosTest<object>(1, (object)UInt32.MaxValue, UInt32.MaxValue); }
    public bool PosTest2() { return PosTest<object>(2, (object)UInt32.MinValue, UInt32.MinValue); }
    public bool PosTest3() { return PosTest<object>(3, null, 0); }

    public bool PosTest4() { return PosTest<Boolean>(4, true, 1); }
    public bool PosTest5() { return PosTest<Boolean>(5, false, 0); }

    public bool PosTest6() { return PosTest2(6, null, 0); }
    public bool PosTest7() { return PosTest2(7, "1034", 1034); }

    public bool PosTest8() { return PosTest2(8, null, 0); }
    public bool PosTest9() { return PosTest2(9, "34", 34); }

    public bool PosTest10() { return PosTest<double>(10, 4294967294.5d, 4294967294); }

    public bool PosTest<T>(int id, T curValue, UInt32 expValue)
    {
        bool            retVal = true;
        UInt32         newValue;
        IFormatProvider myfp;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ToUInt32(...) (curValue:"+typeof(T)+" " +curValue+" newType:"+expValue.GetType()+")");

        try
        {
            newValue = Convert.ToUInt32(curValue);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("000", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                retVal = false;
            }

            myfp     = null;
            newValue = Convert.ToUInt32(curValue, myfp);

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

    public bool PosTest2(int id, String curValue, UInt32 expValue)
    {
        bool            retVal = true;
        UInt32         newValue;
        IFormatProvider myfp;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ToUInt32(...) (curValue:string " +curValue+" newType:"+expValue.GetType()+")");

        try
        {
            newValue = Convert.ToUInt32(curValue);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("003", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                retVal = false;
            }

            myfp     = null;
            newValue = Convert.ToUInt32(curValue, myfp);

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
}

