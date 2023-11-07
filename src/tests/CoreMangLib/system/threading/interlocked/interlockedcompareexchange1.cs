// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

// This test makes sure that CompareExchange<T> works
// where T is string
public class InterlockedCompareExchange1
{
    private const int c_NUM_LOOPS = 100;
    private const int c_MIN_STRING_LEN = 64;
    private const int c_MAX_STRING_LEN = 1024;

    [Fact]
    public static int TestEntryPoint()
    {
        InterlockedCompareExchange1 test = new InterlockedCompareExchange1();

        TestLibrary.TestFramework.BeginTestCase("InterlockedCompareExchange1");

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
        retVal = PosTest1<string>() && retVal;
        retVal = PosTest2<string>() && retVal;

        return retVal;
    }

    // CompareExchange of equal strings as T
    public bool PosTest1<T>() where T : class
    {
        bool retVal = true;
        T   location;
        T   value;
        T   comparand;
        T   oldLocation;

        TestLibrary.TestFramework.BeginScenario("PosTest1: T Interlocked.CompareExchange(T&,T,T) (T=" + typeof(T) + ") where comparand is equal");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                value       = (T)(object)TestLibrary.Generator.GetString(false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
                location    = (T)(object)TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
                comparand   = location;
     
                // location=comparand, so location should be replaced by value and
                // oldLocation should equal comparand
                oldLocation = Interlocked.CompareExchange<T>(ref location, value, comparand);

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

    // CompareExchange of unequal strings as T
    public bool PosTest2<T>() where T : class
    {
        bool retVal = true;
        T   location;
        T   value;
        T   comparand;
        T   oldLocation;

        TestLibrary.TestFramework.BeginScenario("PosTest2: T Interlocked.CompareExchange(T&,T,T) (T=" + typeof(T) + ") where comparand are not equal");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                value       = (T)(object)TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
                location    = (T)(object)TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
                comparand   = value;
                while(comparand.Equals(location))
                {
                    comparand = (T)(object)TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
                }

                // location!=comparand, so no swap should take place.
                // location should not be replaced by value 
                // oldLocation should not equal comparand
                oldLocation = Interlocked.CompareExchange<T>(ref location, value, comparand);

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
