// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

public class InterlockedCompareExchange6
{
    private const int c_NUM_LOOPS = 100;

    [Fact]
    public static int TestEntryPoint()
    {
        InterlockedCompareExchange6 test = new InterlockedCompareExchange6();

        TestLibrary.TestFramework.BeginTestCase("InterlockedCompareExchange6");

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
        bool   retVal = true;
        Int32 location;
        Int32 value;
        Int32 comparand;
        Int32 oldLocation;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Int32 Interlocked.CompareExchange(Int32&,Int32,Int32) where comparand is equal");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                value       = TestLibrary.Generator.GetInt32(-55);
                location    = TestLibrary.Generator.GetInt32(-55);
                comparand   = location;
     
                oldLocation = Interlocked.CompareExchange(ref location, value, comparand);

                if (!location.Equals(value))
                {
                    TestLibrary.TestFramework.LogError("001", "Interlocked.CompareExchange() did not do the exchange correctly: Expected(" + value + ") Actual(" + location + ")");
                    retVal = false;
                }

                if (!oldLocation.Equals(comparand))
                {
                    TestLibrary.TestFramework.LogError("002", "Interlocked.CompareExchange() did not return the expected value: Expected(" + comparand + ") Actual(" + oldLocation + ")");
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

    public bool PosTest2()
    {
        bool   retVal = true;
        Int32 location;
        Int32 value;
        Int32 comparand;
        Int32 oldLocation;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Int32 Interlocked.CompareExchange(Int32&,Int32,Int32) where comparand are not equal");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                value       = TestLibrary.Generator.GetInt32(-55);
                location    = TestLibrary.Generator.GetInt32(-55);
                comparand   = value;
                while(comparand.Equals(location))
                {
                    comparand = TestLibrary.Generator.GetInt32(-55);
                }
     
                oldLocation = Interlocked.CompareExchange(ref location, value, comparand);

                if (location.Equals(value))
                {
                    TestLibrary.TestFramework.LogError("004", "Interlocked.CompareExchange() did not do the exchange correctly: Expected(" + value + ") Actual(" + location + ")");
                    retVal = false;
                }

                if (oldLocation.Equals(comparand))
                {
                    TestLibrary.TestFramework.LogError("005", "Interlocked.CompareExchange() did not return the expected value: Expected(" + comparand + ") Actual(" + oldLocation + ")");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

}
