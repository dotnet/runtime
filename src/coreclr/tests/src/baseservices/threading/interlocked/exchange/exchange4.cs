// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

// Tests that Exchange(Single, Single)
// a.k.a., CompareExchange(float, float)
// actually switches values
public class InterlockedExchange4
{
    private const int c_NUM_LOOPS = 100;

    public static int Main()
    {
        InterlockedExchange4 test = new InterlockedExchange4();

        TestLibrary.TestFramework.BeginTestCase("InterlockedExchange4");

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
        Single location;
        Single value;
        Single prevLocation;
        Single oldLocation;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Single Interlocked.Exchange(Single&,Single)");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                value       = TestLibrary.Generator.GetSingle();
                location    = TestLibrary.Generator.GetSingle();
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
