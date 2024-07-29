// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

public class InterlockedIncrement2
{
    private const int c_NUM_LOOPS = 100;

    [Fact]
    public static int TestEntryPoint()
    {
        InterlockedIncrement2 test = new InterlockedIncrement2();

        TestLibrary.TestFramework.BeginTestCase("InterlockedIncrement2");

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
        Int64 value;
        Int64 nwValue;
        Int64 exValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Int64 Interlocked.Increment(Int64&)");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                value   = TestLibrary.Generator.GetInt64(-55);
     
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
        Int64 value;
        Int64 nwValue;
        Int64 exValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Cause a positive Int64 overflow");

        try
        {
            value    = Int64.MaxValue;
     
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

    public bool CheckValues(Int64 value, Int64 exValue, Int64 nwValue)
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
