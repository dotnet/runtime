// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

public class AutoResetEventSet
{
    public static int Main()
    {
        AutoResetEventSet test = new AutoResetEventSet();

        TestLibrary.TestFramework.BeginTestCase("AutoResetEventSet");

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: AutoResetEvent.Set()");

        try
        {
            // false means that the initial state should be not signaled
            are = new AutoResetEvent(false);

            // set the signal
            if (!are.Set())
            {
                TestLibrary.TestFramework.LogError("001", "AutoResetEvent.Set() failed");
                retVal = false;
            }

            // verify that the autoreset event is signaled
            // if it is not signaled the following call will block for ever
            TestLibrary.TestFramework.LogInformation("Calling AutoResetEvent.WaitOne()... if the event is not signaled it will hang");
            are.WaitOne();

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

}
