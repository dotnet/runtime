// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

// Tests that CompareExchange(Int64, Int64, Int64)
// actually switches values when location = comparand and
// does not when it does not
public class InterlockedCompareExchange5
{
    private const int c_NUM_LOOPS = 100;

    [Fact]
    public static int TestEntryPoint()
    {
        InterlockedCompareExchange5 test = new InterlockedCompareExchange5();

        TestLibrary.TestFramework.BeginTestCase("InterlockedCompareExchange5");

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

    // This test iterates 100 times.  Each time it gets two 
    // Int64s, value and location.  It stores location in comparand, so 
    // they will be equal on comparison.  It then uses 
    // Interlocked.CompareExchange to compare location with comparand.  
    // Since they are equal, it must exchange:  location is replaced by 
    // value, and the original value in location is returned to oldLocation. 
    // Then it checks that location now equals value, and that oldLocation
    // equals comparand.  If either of these are not true, retVal gets set to 
    // false, and ultimately it returns false.  since location is never null,
    // it should not throw an exception, so doing so causes the test 
    // to fail.
    public bool PosTest1()
    {
        bool   retVal = true;
        Int64 location;
        Int64 value;
        Int64 comparand;
        Int64 oldLocation;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Int64 Interlocked.CompareExchange(Int64&,Int64,Int64) where comparand is equal");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                value       = TestLibrary.Generator.GetInt64(-55);
                location    = TestLibrary.Generator.GetInt64(-55);
                comparand   = location;
     
                oldLocation = Interlocked.CompareExchange(ref location, value, comparand);
                // At this point, we should be able to make 
                // the following assertion:
                // location = value
                // oldLocation = comparand

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
            // since location is not null, any exception 
            // causes test failure.
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    // This test iterates 100 times.  Each time it gets two 
    // Int64s, value and location.  It then gets a value for comparand, such 
    // that it is NOT EQUAL to location.  It then uses 
    // Interlocked.CompareExchange to compare location with comparand.  
    // Since they are not equal, it does not exchange.
    // since location is never null,
    // it should not throw an exception, so doing so causes the test 
    // to fail.
    public bool PosTest2()
    {
        bool   retVal = true;
        Int64 location;
        Int64 value;
        Int64 comparand;
        Int64 oldLocation;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Int64 Interlocked.CompareExchange(Int64&,Int64,Int64) where comparand are not equal");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                value       = TestLibrary.Generator.GetInt64(-55);
                location    = TestLibrary.Generator.GetInt64(-55);
                comparand   = value;
                while(comparand.Equals(location))
                {
                    comparand = TestLibrary.Generator.GetInt64(-55);
                }
     
                oldLocation = Interlocked.CompareExchange(ref location, value, comparand);
                // At this point, we should be able to make 
                // the following assertions:
                // location != value
                // location != comparand
                // location = oldLocation
                // oldLocation != comparand

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
            // since location is not null, any exception 
            // causes test failure.
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

}
