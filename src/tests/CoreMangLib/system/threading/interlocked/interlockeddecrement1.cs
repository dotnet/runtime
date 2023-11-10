// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

public class InterlockedDecrement1
{
    private const int c_NUM_LOOPS = 100;

    [Fact]
    public static int TestEntryPoint()
    {
        InterlockedDecrement1 test = new InterlockedDecrement1();

        TestLibrary.TestFramework.BeginTestCase("InterlockedDecrement1");

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Int32 Interlocked.Decrement(Int32&)");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                value   = TestLibrary.Generator.GetInt32(-55);
     
                exValue = value-1;
                nwValue = Interlocked.Decrement(ref value);

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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Cause a negative Int32 overflow");

        try
        {
            value    = Int32.MinValue;
     
            exValue = value-1;
            nwValue = Interlocked.Decrement(ref value);

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
            TestLibrary.TestFramework.LogError("003", "Interlocked.Decrement() returned wrong value. Expected(" + exValue + ") Got(" + nwValue + ")");
            return false;
        }
        if (exValue != value)
        {
            TestLibrary.TestFramework.LogError("003", "Interlocked.Decrement() did not update value. Expected(" + exValue + ") Got(" + value + ")");
            return false;
        }

        return true;
    }

}
