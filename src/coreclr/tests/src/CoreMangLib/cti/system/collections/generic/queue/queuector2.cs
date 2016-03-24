// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// ctor(System.Collections.Generic.IEnumerable<T>)
/// </summary>

public class QueueCtor2
{
    public static int Main()
    {
        QueueCtor2 test = new QueueCtor2();

        TestLibrary.TestFramework.BeginTestCase("QueueCtor2");

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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Test whether ctor(IEnumerable<T>) is successful when passing string array.");

        try
        {
            string[] TestArray = { "first", "second" };
            Queue<string> TestQueue = new Queue<string>(TestArray);
            if (TestQueue == null || TestQueue.Count != 2)
            {
                TestLibrary.TestFramework.LogError("P01.1", "ctor(IEnumerable<T>) failed when passing string array!");
                retVal = false;
            }
            string element1 = TestQueue.Dequeue();
            string element2 = TestQueue.Dequeue();
            if (element1 != "first" || element2 != "second")
            {
                TestLibrary.TestFramework.LogError("P01.2", "ctor(IEnumerable<T>) failed when passing string array!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P01.3", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Test whether ctor(IEnumerable<T>) is successful when passing int array.");

        try
        {
            int[] TestArray = { 20, 30 };
            Queue<int> TestQueue = new Queue<int>(TestArray);
            if (TestQueue == null || TestQueue.Count != 2)
            {
                TestLibrary.TestFramework.LogError("P02.1", "ctor(IEnumerable<T>) failed when passing int array!");
                retVal = false;
            }
            int element1 = TestQueue.Dequeue();
            int element2 = TestQueue.Dequeue();
            if (element1 != 20 || element2 != 30)
            {
                TestLibrary.TestFramework.LogError("P02.2", "ctor(IEnumerable<T>) failed when passing int array!");
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

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Test whether ctor(IEnumerable<T>) is successful when passing an empty array.");

        try
        {
            string[] TestArray = { };
            Queue<string> TestQueue = new Queue<string>(TestArray);
            if (TestQueue == null || TestQueue.Count != 0)
            {
                TestLibrary.TestFramework.LogError("P03.1", "ctor(IEnumerable<T>) failed when passing an empty array!");
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

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException should be thrown when collection is a null reference.");

        try
        {
            Queue<string> TestQueue = new Queue<string>(null);
            TestLibrary.TestFramework.LogError("N01.1", "ArgumentNullException is not thrown when collection is a null reference!");
            retVal = false;
        }
        catch (ArgumentNullException)
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
