// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ConvertBoolean
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;
    private const int c_MIN_STRLEN = 1;
    private const int c_MAX_STRLEN = 1024;

    public static int Main()
    {
        ConvertBoolean ac = new ConvertBoolean();

        TestLibrary.TestFramework.BeginTestCase("Convert.ToBoolean(...)");

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

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1()  && retVal;

        return retVal;
    }

    // public static bool ToBoolean(Object value)
    public bool PosTest1() { return PosTest<object>(1, (object)true, true); }
    public bool PosTest2() { return PosTest<object>(2, (object)false, false); }
    public bool PosTest3() { return PosTest<object>(3, (object)null, false); }

    // public static bool ToBoolean(String value) {
    public bool PosTest4() { return PosTest<string>(4, "True", true); }
    public bool PosTest5() { return PosTest<string>(5, "False", false); }
    public bool PosTest6() { return PosTest<string>(6, null, false); }

    public bool NegTest1() { return NegTest<char>(1, 'c', typeof(System.InvalidCastException)); }

    public bool PosTest<T>(int id, T curValue, Boolean expValue)
    {
        bool            retVal = true;
        Boolean         newValue;
        IFormatProvider fp;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ToBoolean(...) (curValue:"+typeof(T)+ " " +curValue+" newType:"+expValue.GetType()+")");

        try
        {
            newValue = Convert.ToBoolean(curValue);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("000", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                retVal = false;
            }

            fp = null;
            newValue = Convert.ToBoolean(curValue, fp);

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
        bool            retVal = true;
        Boolean         newValue;

        TestLibrary.TestFramework.BeginScenario("NegTest"+id+": Convert.ToBoolean(...) (curValue:"+typeof(T)+ " " +curValue+")");

        try
        {
            newValue = Convert.ToBoolean(curValue);

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
