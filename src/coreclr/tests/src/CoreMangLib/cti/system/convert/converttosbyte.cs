// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ConvertToSByte
{
    public static int Main()
    {
        ConvertToSByte ac = new ConvertToSByte();

        TestLibrary.TestFramework.BeginTestCase("Convert.ToSByte(...)");

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

        TestLibrary.TestFramework.LogInformation("");
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1()  && retVal;
        retVal = NegTest2()  && retVal;
        retVal = NegTest3()  && retVal;

        return retVal;
    }

    public bool PosTest1() { return PosTest<object>(1, (object)SByte.MaxValue, SByte.MaxValue); }
    public bool PosTest2() { return PosTest<object>(2, (object)SByte.MinValue, SByte.MinValue); }
    public bool PosTest3() { return PosTest<object>(3, null, 0); }

    public bool PosTest4() { return PosTest2(4, "27", 27); }

    public bool NegTest1() { return NegTest1(1, Int16.MaxValue.ToString(), 10, typeof(System.OverflowException)); }
    public bool NegTest2() { return NegTest1(2, Int16.MinValue.ToString(), 10, typeof(System.OverflowException)); }
    public bool NegTest3() { return NegTest2(3, null, 10, typeof(System.ArgumentNullException)); }

    public bool PosTest<T>(int id, T curValue, SByte expValue)
    {
        bool            retVal = true;
        SByte         newValue;
        IFormatProvider myfp;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ToSByte(...) (curValue:"+typeof(T)+" " +curValue+" newType:"+expValue.GetType()+")");

        try
        {
            newValue = Convert.ToSByte(curValue);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("000", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                retVal = false;
            }

            myfp     = null;
            newValue = Convert.ToSByte(curValue, myfp);

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

    public bool PosTest2(int id, String curValue, SByte expValue)
    {
        bool            retVal = true;
        SByte         newValue;
        IFormatProvider myfp;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ToSByte(...) (curValue:string " +curValue+" newType:"+expValue.GetType()+")");

        try
        {
            newValue = Convert.ToSByte(curValue);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("003", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                retVal = false;
            }

            myfp     = null;
            newValue = Convert.ToSByte(curValue, myfp);

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

    public bool NegTest1(int id, string curValue, int fromBase, Type exception)
    {
        bool            retVal = true;
        SByte         newValue;

        TestLibrary.TestFramework.BeginScenario("NegTest"+id+": Convert.ToSByte(...) (curValue:string " +curValue+" base:"+fromBase+")");

        try
        {
            newValue = Convert.ToSByte(curValue, fromBase);

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

    public bool NegTest2(int id, string curValue, int fromBase, Type exception)
    {
        bool            retVal = true;
        SByte         newValue;
        IFormatProvider myfp;

        TestLibrary.TestFramework.BeginScenario("NegTest"+id+": Convert.ToSByte(...) (curValue:string " +curValue+" base:"+fromBase+")");

        try
        {
            myfp     = null;
            newValue = Convert.ToSByte(curValue, myfp);

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

