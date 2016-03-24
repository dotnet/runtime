// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ConvertToByte
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;
    private const int c_MIN_STRLEN = 1;
    private const int c_MAX_STRLEN = 1024;

    public static int Main()
    {
        ConvertToByte ac = new ConvertToByte();

        TestLibrary.TestFramework.BeginTestCase("Convert.ToByte(...)");

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

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1()  && retVal;
        retVal = NegTest2()  && retVal;
        retVal = NegTest3()  && retVal;
        retVal = NegTest4()  && retVal;
        retVal = NegTest5()  && retVal;
        retVal = NegTest6()  && retVal;
        retVal = NegTest7()  && retVal;
        retVal = NegTest8()  && retVal;
        retVal = NegTest9()  && retVal;

        return retVal;
    }

    // public static byte ToByte (String value, int fromBase)
    public bool PosTest1() { return PosTest1(1, Byte.MaxValue.ToString(), Byte.MaxValue, 10); }
    public bool PosTest2() { return PosTest1(2, Byte.MinValue.ToString(), Byte.MinValue,  10); }
    public bool PosTest3() { return PosTest1(3, "101", 5, 2); }
    public bool PosTest4() { return PosTest1(4, "75", 61, 8); }
    public bool PosTest5() { return PosTest1(5, "a5", 165, 16); }
    public bool PosTest6() { return PosTest1(6, null, 0, 10); }

    // public static byte ToByte(object value) {
    public bool PosTest7() { return PosTest2(7, null, 0); }
    public bool PosTest8() { return PosTest2(8, "34", 34); }

                                                //id string  base  exception
    public bool NegTest1() { return NegTest1(1, "",      1, typeof(System.ArgumentException)); }
    public bool NegTest2() { return NegTest1(2, "",      3, typeof(System.ArgumentException)); }
    public bool NegTest3() { return NegTest1(3, "",    100, typeof(System.ArgumentException)); }
    public bool NegTest4() { return NegTest1(4, Int32.MaxValue.ToString(),    10, typeof(System.OverflowException)); }
    public bool NegTest5() { return NegTest1(5, Int32.MinValue.ToString(),    10, typeof(System.OverflowException)); }
    public bool NegTest6() { return NegTest1(6, "2",    2, typeof(System.FormatException)); }
    public bool NegTest7() { return NegTest1(7, "8",    8, typeof(System.FormatException)); }
    public bool NegTest8() { return NegTest1(8, "a",    10, typeof(System.FormatException)); }
    public bool NegTest9() { return NegTest1(9, "g",    16, typeof(System.FormatException)); }

    public bool PosTest1(int id, string curValue, Byte expValue, int fromBase)
    {
        bool            retVal = true;
        Byte         newValue;
        IFormatProvider myfp;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ToByte(...) (curValue:string " +curValue+" newType:"+expValue.GetType()+")");

        try
        {
            newValue = Convert.ToByte(curValue, fromBase);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("000", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                retVal = false;
            }

            if (10 == fromBase)
            {
                newValue = Convert.ToByte(curValue);

                if (!newValue.Equals(expValue))
                {
                    TestLibrary.TestFramework.LogError("001", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                    retVal = false;
                }

                myfp     = null;
                newValue = Convert.ToByte(curValue, myfp);

                if (!newValue.Equals(expValue))
                {
                    TestLibrary.TestFramework.LogError("002", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                    retVal = false;
                }
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2(int id, object curValue, Byte expValue)
    {
        bool            retVal = true;
        Byte         newValue;
        IFormatProvider myfp;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ToByte(...) (curValue:object " +curValue+" newType:"+expValue.GetType()+")");

        try
        {
            newValue = Convert.ToByte(curValue);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("004", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                retVal = false;
            }

            myfp     = null;
            newValue = Convert.ToByte(curValue, myfp);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("005", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
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

    public bool NegTest1(int id, string curValue, int fromBase, Type exception)
    {
        bool            retVal = true;
        Byte         newValue;

        TestLibrary.TestFramework.BeginScenario("NegTest"+id+": Convert.ToByte(...) (curValue:string " +curValue+" base:"+fromBase+")");

        try
        {
            newValue = Convert.ToByte(curValue, fromBase);

            TestLibrary.TestFramework.LogError("007", "Exception expected");
            retVal = false;
        }
        catch (Exception e)
        {
            if (e.GetType() != exception)
            {
                TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
                retVal = false;
            }
        }

        return retVal;
    }
}
