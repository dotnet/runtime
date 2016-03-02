// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// ToArray()
/// </summary>

public class QueueToArray
{
    public static int Main()
    {
        QueueToArray test = new QueueToArray();

        TestLibrary.TestFramework.BeginTestCase("QueueToArray");

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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Test whether ToArray() is successful when the queue is empty.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            string[] TestArray = TestQueue.ToArray();
            if (TestArray.Length != 0)
            {
                TestLibrary.TestFramework.LogError("P01.1", "ToArray() failed when the queue is empty!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Test whether ToArray() is successful when the queue is not empty.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            string[] TestArray = TestQueue.ToArray();
            if (TestArray.Length != 2)
            {
                TestLibrary.TestFramework.LogError("P02.1", "ToArray() failed! Element number in array and queue are not equal!");
                retVal = false;
            }
            if (TestArray[0] != "one" || TestArray[1] != "two")
            {
                TestLibrary.TestFramework.LogError("P02.2", "ToArray() failed! Elements in array are not the same as in queue!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P02.3", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
