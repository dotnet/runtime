// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// Current()
/// </summary>

public class EnumeratorCurrent
{
    public static int Main()
    {
        EnumeratorCurrent test = new EnumeratorCurrent();

        TestLibrary.TestFramework.BeginTestCase("EnumeratorCurrent");

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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Current() should be successful.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            TestQueue.Enqueue("three");
            Queue<string>.Enumerator TestEnumerator = TestQueue.GetEnumerator();
            TestEnumerator.MoveNext();
            if (TestEnumerator.Current != "one")
            {
                TestLibrary.TestFramework.LogError("P01.1", "Current() failed!");
                retVal = false;
            }
            TestEnumerator.MoveNext();
            if (TestEnumerator.Current != "two")
            {
                TestLibrary.TestFramework.LogError("P01.2", "Current() failed!");
                retVal = false;
            }
            TestEnumerator.MoveNext();
            if (TestEnumerator.Current != "three")
            {
                TestLibrary.TestFramework.LogError("P01.3", "Current() failed!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P01.4", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Current() should be successful even if the queue has been modified.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            TestQueue.Enqueue("three");
            Queue<string>.Enumerator TestEnumerator = TestQueue.GetEnumerator();
            TestEnumerator.MoveNext();
            if (TestEnumerator.Current != "one")
            {
                TestLibrary.TestFramework.LogError("P02.1", "Current() failed!");
                retVal = false;
            }
            TestEnumerator.MoveNext();
            TestQueue.Dequeue();
            if (TestEnumerator.Current != "two")
            {
                TestLibrary.TestFramework.LogError("P02.2", "Current() failed!");
                retVal = false;
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

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: InvalidOperationException should be thrown when the enumerator is positioned before the first element of the collection.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            TestQueue.Enqueue("three");
            Queue<string>.Enumerator TestEnumerator;
            TestEnumerator = TestQueue.GetEnumerator();
            string TestString = TestEnumerator.Current;
            TestLibrary.TestFramework.LogError("N01.1", "InvalidOperationException is not thrown when the enumerator is positioned before the first element of the collection!");
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

    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2: InvalidOperationException should be thrown when the enumerator is positioned after the last element of the collection.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            TestQueue.Enqueue("three");
            Queue<string>.Enumerator TestEnumerator;
            TestEnumerator = TestQueue.GetEnumerator();
            TestEnumerator.MoveNext();
            TestEnumerator.MoveNext();
            TestEnumerator.MoveNext();
            TestEnumerator.MoveNext();
            string TestString = TestEnumerator.Current;
            TestLibrary.TestFramework.LogError("N02.1", "InvalidOperationException is not thrown when the enumerator is positioned after the last element of the collection!");
            retVal = false;
        }
        catch (InvalidOperationException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N02.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest3: InvalidOperationException should be thrown when MoveNext after queue has been changed.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            TestQueue.Enqueue("three");
            Queue<string>.Enumerator TestEnumerator = TestQueue.GetEnumerator();
            TestEnumerator.MoveNext();
            if (TestEnumerator.Current != "one")
            {
                TestLibrary.TestFramework.LogError("P02.1", "Current() failed!");
                retVal = false;
            }
            TestQueue.Dequeue();
            TestEnumerator.MoveNext();
            TestLibrary.TestFramework.LogError("P02.1", "InvalidOperationException not thrown when MoveNext after queue has been changed!");
            retVal = false;
        }
        catch (InvalidOperationException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N03.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
