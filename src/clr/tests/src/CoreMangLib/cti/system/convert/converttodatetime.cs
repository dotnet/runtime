// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Globalization;

public class ConvertToDateTime
{
    public static int Main()
    {
        ConvertToDateTime ac = new ConvertToDateTime();

        TestLibrary.TestFramework.BeginTestCase("Convert.ToDateTime(...)");

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
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;

        return retVal;
    }

    // public static DateTime ToDateTime(object value)
    public bool PosTest1() { DateTime now=DateTime.Now; return PosTest<object>(1, now.ToString(), now); }
    public bool PosTest2() { return PosTest<object>(2, DateTime.MinValue, DateTime.MinValue); }
    public bool PosTest3() { return PosTest<object>(3, CultureInfo.CurrentCulture.Calendar.MaxSupportedDateTime, CultureInfo.CurrentCulture.Calendar.MaxSupportedDateTime); }
    public bool PosTest4() { return PosTest<object>(4, (object)null, DateTime.MinValue); }

    // public static DateTime ToDateTime(String value, IFormatProvider provider) {

    public bool PosTest5() { return PosTest2(5, null, new DateTime(0)); }
    public bool PosTest6() { DateTime now=DateTime.Now; return PosTest2(6, now.ToString(), now); }

    public bool PosTest<T>(int id, T curValue, DateTime expValue)
    {
        bool            retVal = true;
        DateTime        newValue;
        IFormatProvider myfp;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ToDateTime(...) (curValue:"+typeof(T)+" " +curValue+" newType:"+expValue.GetType()+")");

        try
        {
            newValue = Convert.ToDateTime(curValue);

            if (9999999 <= Math.Abs(newValue.Subtract(expValue).Ticks))
            {
                TestLibrary.TestFramework.LogError("000", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ") Diff("+newValue.Subtract(expValue).Ticks+")");
                retVal = false;
            }

            myfp     = null;
            newValue = Convert.ToDateTime(curValue, myfp);

            if (9999999 <= Math.Abs(newValue.Subtract(expValue).Ticks))
            {
                TestLibrary.TestFramework.LogError("001", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ") Diff("+newValue.Subtract(expValue).Ticks+")");
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

    public bool PosTest2(int id, string curValue, DateTime expValue)
    {
        bool            retVal = true;
        DateTime        newValue;
        IFormatProvider myfp;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ToDateTime(...) (curValue:string " +curValue+" newType:"+expValue.GetType()+")");

        try
        {
            newValue = Convert.ToDateTime(curValue);

            if (9999999 <= Math.Abs(newValue.Subtract(expValue).Ticks))
            {
                TestLibrary.TestFramework.LogError("003", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ") Diff("+newValue.Subtract(expValue).Ticks+")");
                retVal = false;
            }

            myfp     = null;
            newValue = Convert.ToDateTime(curValue, myfp);

            if (9999999 <= Math.Abs(newValue.Subtract(expValue).Ticks))
            {
                TestLibrary.TestFramework.LogError("004", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ") Diff("+newValue.Subtract(expValue).Ticks+")");
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
