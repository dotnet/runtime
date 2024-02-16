// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

public class InterlockedExchange6
{
    private const int c_NUM_LOOPS = 100;

    [Fact]
    public static int TestEntryPoint()
    {
        InterlockedExchange6 test = new InterlockedExchange6();

        TestLibrary.TestFramework.BeginTestCase("InterlockedExchange6");

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
        Int32 location;
        Int32 value;
        Int32 prevLocation;
        Int32 oldLocation;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Int32 Interlocked.Exchange(Int32&,Int32)");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                value       = TestLibrary.Generator.GetInt32(-55);
                location    = TestLibrary.Generator.GetInt32(-55);
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
