using System;
using System.Threading;

public class InterlockedIncrement1
{
    private const int c_NUM_LOOPS = 100;

    public static int Main()
    {
        InterlockedIncrement1 test = new InterlockedIncrement1();

        TestLibrary.TestFramework.BeginTestCase("InterlockedIncrement1");

        if (test.RunTests())
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool  retVal = true;
        Int32 value;
        Int32 nwValue;
        Int32 exValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Int32 Interlocked.Increment(Int32&)");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                value   = TestLibrary.Generator.GetInt32(-55);
     
                exValue = value+1;
                nwValue = Interlocked.Increment(ref value);

                retVal = CheckValues(value, exValue, nwValue) && retVal;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool           retVal = true;
        Int32 value;
        Int32 nwValue;
        Int32 exValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Cause a positive Int32 overflow");

        try
        {
            value    = Int32.MaxValue;
     
            exValue = value+1;
            nwValue = Interlocked.Increment(ref value);

            retVal = CheckValues(value, exValue, nwValue) && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool CheckValues(Int32 value, Int32 exValue, Int32 nwValue)
    {
        if (exValue != nwValue)
        {
            TestLibrary.TestFramework.LogError("003", "Interlocked.Increment() returned wrong value. Expected(" + exValue + ") Got(" + nwValue + ")");
            return false;
        }
        if (exValue != value)
        {
            TestLibrary.TestFramework.LogError("003", "Interlocked.Increment() did not update value. Expected(" + exValue + ") Got(" + value + ")");
            return false;
        }

        return true;
    }

}
