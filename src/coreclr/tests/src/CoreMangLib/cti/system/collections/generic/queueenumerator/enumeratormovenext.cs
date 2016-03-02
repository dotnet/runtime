// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// MoveNext()
/// </summary>

public class EnumeratorMoveNext
{
    public static int Main()
    {
        EnumeratorMoveNext test = new EnumeratorMoveNext();

        TestLibrary.TestFramework.BeginTestCase("EnumeratorMoveNext");

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
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: The enumerator should be positioned at the first element in the collection after MoveNext().");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            TestQueue.Enqueue("three");
            Queue<string>.Enumerator TestEnumerator;
            TestEnumerator = TestQueue.GetEnumerator();
            TestEnumerator.MoveNext();
            string TestString = TestEnumerator.Current;
            if (TestString != "one")
            {
                TestLibrary.TestFramework.LogError("P01.1", "The enumerator is not positioned at the first element in the collection after MoveNext()!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: The enumerator should be positioned at the next element in the collection after MoveNext().");

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
            TestLibrary.TestFramework.LogError("P02.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: The enumerator should be positioned after the last element in the collection after MoveNext() passed the end of collection.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            TestQueue.Enqueue("three");
            Queue<string>.Enumerator TestEnumerator;
            TestEnumerator = TestQueue.GetEnumerator();
            bool bSuc;
            bSuc = TestEnumerator.MoveNext();
            bSuc = TestEnumerator.MoveNext();
            bSuc = TestEnumerator.MoveNext();
            bSuc = TestEnumerator.MoveNext();
            bSuc = TestEnumerator.MoveNext();
            bSuc = TestEnumerator.MoveNext();
            if (bSuc)
            {
                TestLibrary.TestFramework.LogError("P03.1", "The enumerator is not positioned after the last element in the collection after MoveNext() passed the end of collection!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P03.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: With a Queue that contains three items, MoveNext returns true the first three times and returns false every time it is called after that.");

        try
        {
            bool b;
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            TestQueue.Enqueue("three");
            Queue<string>.Enumerator TestEnumerator;
            TestEnumerator = TestQueue.GetEnumerator();
            for (int i = 0; i < 3; i++)
            {
                b = TestEnumerator.MoveNext();
                if (!b)
                {
                    TestLibrary.TestFramework.LogError("P04.1", "MoveNext() failed!");
                    retVal = false;
                }
            }
            b = TestEnumerator.MoveNext();
            if (b)
            {
                TestLibrary.TestFramework.LogError("P04.1", "MoveNext() failed!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P04.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: InvalidOperationException should be thrown when the collection was modified after the enumerator was created.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            TestQueue.Enqueue("three");
            Queue<string>.Enumerator TestEnumerator;
            TestEnumerator = TestQueue.GetEnumerator();
            TestQueue.Enqueue("four");
            TestEnumerator.MoveNext();
            TestLibrary.TestFramework.LogError("N01.1", "InvalidOperationException is not thrown when the collection was modified after the enumerator was created!");
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
