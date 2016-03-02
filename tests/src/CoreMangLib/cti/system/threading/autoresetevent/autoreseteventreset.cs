// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

public class AutoResetEventReset
{
    private const int  c_MILLISECONDS_TOWAIT = 5000;	// milliseconds
    private const long c_DELTA               = 9999999;	// ticks  (0.999 seconds)

    public static int Main()
    {
        AutoResetEventReset test = new AutoResetEventReset();

        TestLibrary.TestFramework.BeginTestCase("AutoResetEventReset");

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
        bool           retVal = true;
        AutoResetEvent are;
	long           ticksBefore;
	long           ticksAfter;

        TestLibrary.TestFramework.BeginScenario("PosTest1: AutoResetEvent.Reset()");

        try
        {
            // true means that the initial state should be signaled
            are = new AutoResetEvent(true);
 
            if (!are.Reset())
            {
                TestLibrary.TestFramework.LogError("001", "AutoResetEvent.Reset() failed");
                retVal = false;
            }

            ticksBefore = DateTime.Now.Ticks;

            // verify that the autoreset event is signaled
            // if it is not signaled the following call will block for ever
            TestLibrary.TestFramework.LogInformation("Calling AutoResetEvent.WaitOne()... if the event is signaled it will not wait long enough");
            are.WaitOne(c_MILLISECONDS_TOWAIT);

            ticksAfter = DateTime.Now.Ticks;

            if (c_DELTA < Math.Abs((ticksAfter - ticksBefore) - (c_MILLISECONDS_TOWAIT*10000)))
            {
                TestLibrary.TestFramework.LogError("002", "AutoResetEvent did not wait long enough... this implies that the parameter was not respected.");
                TestLibrary.TestFramework.LogError("002", " WaitTime=" + (ticksAfter-ticksBefore) + " (ticks)");
                TestLibrary.TestFramework.LogError("002", " Execpted=" + (c_MILLISECONDS_TOWAIT*10000) + " (ticks)");
                TestLibrary.TestFramework.LogError("002", " Acceptable Delta=" + c_DELTA + " (ticks)");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

}
