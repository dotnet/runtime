// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// GetEnumerator()
/// </summary>

public class QueueGetEnumerator
{
    public static int Main()
    {
        QueueGetEnumerator test = new QueueGetEnumerator();

        TestLibrary.TestFramework.BeginTestCase("QueueGetEnumerator");

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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Test whether GetEnumerator() is successful when the queue is not empty.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            TestQueue.Enqueue("three");
            Queue<string>.Enumerator TestEnumerator;
            TestEnumerator = TestQueue.GetEnumerator();
            TestEnumerator.MoveNext();
            for (int i = 0; i < TestQueue.Count; i++)
            {
                if (TestQueue.ToArray()[i] != TestEnumerator.Current)
                {
                    TestLibrary.TestFramework.LogError("P01.1", "GetEnumerator() failed when the queue is not empty!");
                    retVal = false;
                }
                TestEnumerator.MoveNext();
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Test whether GetEnumerator() is successful when the queue is empty.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            Queue<string>.Enumerator TestEnumerator;
            TestEnumerator = TestQueue.GetEnumerator();
            try
            {
                string CurrentElement = TestEnumerator.Current;
                TestLibrary.TestFramework.LogError("P02.1", "GetEnumerator() failed when the queue is empty!");
                retVal = false;
            }
            catch (InvalidOperationException)
            {

            }
            bool b = TestEnumerator.MoveNext();
            if (b != false)
            {
                TestLibrary.TestFramework.LogError("P02.2", "GetEnumerator() failed when the queue is empty!");
                retVal = false;
            }
            try
            {
                string CurrentElement = TestEnumerator.Current;
                TestLibrary.TestFramework.LogError("P02.3", "GetEnumerator() failed when the queue is empty!");
                retVal = false;
            }
            catch (InvalidOperationException)
            {

            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P02.4", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
