using System;
using System.Collections;

public class ConvertToInt64
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;
    private const int c_MIN_STRLEN = 1;
    private const int c_MAX_STRLEN = 1024;

    public static int Main()
    {
        ConvertToInt64 ac = new ConvertToInt64();

        TestLibrary.TestFramework.BeginTestCase("Convert.ToInt64(...)");

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

        TestLibrary.TestFramework.LogInformation("");

        return retVal;
    }

    public bool PosTest1() { return PosTest<object>(1, (object)Int64.MaxValue, Int64.MaxValue); }
    public bool PosTest2() { return PosTest<object>(2, (object)Int64.MinValue, Int64.MinValue); }
    public bool PosTest3() { return PosTest<object>(3, null, 0); }

    public bool PosTest4() { return PosTest2(4, null, 0); }
    public bool PosTest5() { return PosTest2(5, "1034", 1034); }

    public bool PosTest<T>(int id, T curValue, Int64 expValue)
    {
        bool            retVal = true;
        Int64         newValue;
        IFormatProvider myfp;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ToInt64(...) (curValue:"+typeof(T)+" " +curValue+" newType:"+expValue.GetType()+")");

        try
        {
            newValue = Convert.ToInt64(curValue);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("000", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                retVal = false;
            }

            myfp     = null;
            newValue = Convert.ToInt64(curValue, myfp);

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

    public bool PosTest2(int id, String curValue, Int64 expValue)
    {
        bool            retVal = true;
        Int64         newValue;
        IFormatProvider myfp;

        TestLibrary.TestFramework.BeginScenario("PosTest"+id+": Convert.ToInt64(...) (curValue:string " +curValue+" newType:"+expValue.GetType()+")");

        try
        {
            newValue = Convert.ToInt64(curValue);

            if (!newValue.Equals(expValue))
            {
                TestLibrary.TestFramework.LogError("003", "Value mismatch: Expected(" + expValue + ") Actual(" +newValue + ")");
                retVal = false;
            }

            myfp     = null;
            newValue = Convert.ToInt64(curValue, myfp);

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

