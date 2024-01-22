// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

// Tests that Exchange(Int64, Int64)
// actually switches values
public class InterlockedExchange5
{
    private const int c_NUM_LOOPS = 100;

    [Fact]
    public static int TestEntryPoint()
    {
        InterlockedExchange5 test = new InterlockedExchange5();

        TestLibrary.TestFramework.BeginTestCase("InterlockedExchange5");

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

        return retVal;
    }

    public bool PosTest1()
    {
        bool   retVal = true;
        Int64 location;
        Int64 value;
        Int64 prevLocation;
        Int64 oldLocation;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Int64 Interlocked.Exchange(Int64&,Int64)");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                value       = TestLibrary.Generator.GetInt64(-55);
                location    = TestLibrary.Generator.GetInt64(-55);
                prevLocation   = location;
     
                oldLocation = Interlocked.Exchange(ref location, value);

                if (!location.Equals(value))
                {
                    TestLibrary.TestFramework.LogError("001", "Interlocked.Exchange() did not do the exchange correctly: Expected(" + value + ") Actual(" + location + ")");
                    retVal = false;
                }

                if (!oldLocation.Equals(prevLocation))
                {
                    TestLibrary.TestFramework.LogError("002", "Interlocked.Exchange() did not return the expected value: Expected(" + prevLocation + ") Actual(" + oldLocation + ")");
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

}
