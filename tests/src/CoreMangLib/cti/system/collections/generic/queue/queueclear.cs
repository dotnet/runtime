// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// Clear()
/// </summary>

public class QueueClear
{
    public static int Main()
    {
        QueueClear test = new QueueClear();

        TestLibrary.TestFramework.BeginTestCase("QueueClear");

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
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Test whether Clear() is successful when the queue is not empty.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            TestQueue.Clear();
            if (TestQueue.Count != 0)
            {
                TestLibrary.TestFramework.LogError("P01.1", "Clear() failed when the queue is not empty!");
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

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Test whether Clear() is successful when the queue is empty.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Clear();
            if (TestQueue.Count != 0)
            {
                TestLibrary.TestFramework.LogError("P02.1", "Clear() failed when the queue is empty!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P02.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
