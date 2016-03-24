// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// Dequeue()
/// </summary>

public class QueueDequeue
{
    public static int Main()
    {
        QueueDequeue test = new QueueDequeue();

        TestLibrary.TestFramework.BeginTestCase("QueueDequeue");

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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Test whether Dequeue() is successful when the queue is not empty.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            TestQueue.Enqueue("three");
            string ResultString = TestQueue.Dequeue();
            if (TestQueue.Count != 2 || TestQueue.Peek() != "two" || ResultString != "one")
            {
                TestLibrary.TestFramework.LogError("P01.1", "Dequeue() failed when the queue is not empty!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P01.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: InvalidOperationException should be thrown when Dequeue() from an empty queue.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Dequeue();
            TestLibrary.TestFramework.LogError("N01.1", "InvalidOperationException is not thrown when Dequeue() from an empty queue!");
            retVal = false;
        }
        catch (InvalidOperationException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N01.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
