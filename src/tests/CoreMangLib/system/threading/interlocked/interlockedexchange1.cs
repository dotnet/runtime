// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

public class InterlockedExchange1
{
    private const int c_NUM_LOOPS = 100;
    private const int c_MIN_STRING_LEN = 64;
    private const int c_MAX_STRING_LEN = 1024;

    [Fact]
    public static int TestEntryPoint()
    {
        InterlockedExchange1 test = new InterlockedExchange1();

        TestLibrary.TestFramework.BeginTestCase("InterlockedExchange1");

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

        return retVal;
    }

    public bool PosTest1<T>() where T : class
    {
        bool retVal = true;
        T   location;
        T   value;
        T   prevLocation;
        T   oldLocation;

        TestLibrary.TestFramework.BeginScenario("PosTest1: T Interlocked.Exchange(T&,T) (T=" + typeof(T) + ")");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                value       = (T)(object)TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
                location    = (T)(object)TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
                prevLocation = location;
     
                oldLocation  = Interlocked.Exchange<T>(ref location, value);

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
