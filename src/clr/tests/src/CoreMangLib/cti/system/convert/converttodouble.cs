using System;
using System.Collections;

public class ConvertToDouble
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;
    private const int c_MIN_STRLEN = 1;
    private const int c_MAX_STRLEN = 1024;

    public static int Main()
    {
        ConvertToDouble ac = new ConvertToDouble();

        TestLibrary.TestFramework.BeginTestCase("Convert.ToDouble(...)");

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
        retVal = PosTest11()  && retVal;
        retVal = PosTest12()  && retVal;

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1()  && retVal;
        retVal = NegTest2()  && retVal;

        return retVal;
    }

    // public static Double ToDouble(object value)
    public bool PosTest1() { return PosTest<object>(1, (object)Double.MaxValue, Double.MaxValue); }
    public bool PosTest2() { return PosTest<object>(2, (object)Double.MinValue, Double.MinValue); }
    public bool PosTest3() { return PosTest<object>(3, null, 0); }

    public bool PosTest4() { return PosTest<Boolean>(4, true, 1d); }
    public bool PosTest5() { return PosTest<Boolean>(5, false, 0d); }

    public bool PosTest6() { return PosTest2(6, null, 0d); }
    public bool PosTest7() { return PosTest2(7, "90384230390" + System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + "0384", 90384230390.0384d); }

    // public static byte ToByte(object value) {
    public bool PosTest8() { return PosTest2(8, null, 0); }
    public bool PosTest9() { return PosTest2(9, "34", 34); }

    public bool PosTest10() { return PosTest<object>(10, (object)Double.PositiveInfinity, Double.PositiveInfinity); }
    public bool PosTest11() { return PosTest<object>(11, (object)Double.NegativeInfinity, Double.NegativeInfinity); }
    public bool PosTest12() { return PosTest<object>(12, (object)Double.NaN, Double.NaN); }

    public bool NegTest1() { return NegTest<char>(1, ' ', typeof(System.InvalidCastException)); }
    public bool NegTest2() { return NegTest<DateTime>(2, DateTime.Now, typeof(System.InvalidCastException)); }

    public bool PosTest<T>(int id, T curValue, Double expValue)
    {
        bool            retVal = true;
        Double         newValue;
        IFormatProvider myfp;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ToDouble(...) (curValue:"+typeof(T)+" " +curValue+" newType:"+expValue.GetType()+")");

        try
        {
            newValue = Convert.ToDouble(curValue);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("000", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                retVal = false;
            }

            myfp     = null;
            newValue = Convert.ToDouble(curValue, myfp);

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

    public bool PosTest2(int id, String curValue, Double expValue)
    {
        bool            retVal = true;
        Double         newValue;
        IFormatProvider myfp;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ToDouble(...) (curValue:string " +curValue+" newType:"+expValue.GetType()+")");

        try
        {
            newValue = Convert.ToDouble(curValue);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("003", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                retVal = false;
            }

            myfp     = null;
            newValue = Convert.ToDouble(curValue, myfp);

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

        TestLibrary.TestFramework.BeginScenario("NegTest"+id+": Convert.ToDouble(...) (curValue:"+typeof(T)+" " +curValue+"");

        try
        {
            Convert.ToDouble(curValue);

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
