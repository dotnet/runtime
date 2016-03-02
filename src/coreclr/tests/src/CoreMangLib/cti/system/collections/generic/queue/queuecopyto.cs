// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// CopyTo(T[],System.Int32)
/// </summary>

public class QueueCopyTo
{
    public static int Main()
    {
        QueueCopyTo test = new QueueCopyTo();

        TestLibrary.TestFramework.BeginTestCase("QueueCopyTo");

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
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Test whether CopyTo(T[],System.Int32) is successful when System.Int32 is zero.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            string[] TestArray = { "first", "second", "third", "fourth" };
            TestQueue.CopyTo(TestArray, 0);
            if (TestArray[0] != "one" || TestArray[1] != "two" || TestArray[2] != "third"
                || TestArray[3] != "fourth" || TestArray.GetLength(0) != 4)
            {
                TestLibrary.TestFramework.LogError("P01.1", "CopyTo(T[],System.Int32) failed when System.Int32 is zero!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Test whether CopyTo(T[],System.Int32) is successful when System.Int32 is greater than zero.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            string[] TestArray = { "first", "second", "", "" };
            TestQueue.CopyTo(TestArray, 1);
            if (TestArray.GetLength(0) != 4 || TestArray[0] != "first" || TestArray[1] != "one" || TestArray[2] != "two")
            {
                TestLibrary.TestFramework.LogError("P02.1", "CopyTo(T[],System.Int32) failed when System.Int32 is greater than zero!");
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

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException should be thrown when array is a null reference.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            string[] TestArray = null;
            TestQueue.CopyTo(TestArray, 0);
            TestLibrary.TestFramework.LogError("N01.1", "ArgumentNullException is not thrown when array is a null reference!");
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

    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentOutOfRangeException should be thrown when index is less than zero.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            string[] TestArray = { "first", "second", "", "" };
            TestQueue.CopyTo(TestArray, -1);
            TestLibrary.TestFramework.LogError("N02.1", "ArgumentOutOfRangeException is not thrown when index is less than zero!");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
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
        TestLibrary.TestFramework.BeginScenario("NegTest3: ArgumentException should be thrown when index is equal to the length of array.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            string[] TestArray = { "first", "second", "", "" };
            TestQueue.CopyTo(TestArray, 4);
            TestLibrary.TestFramework.LogError("N03.1", "ArgumentException is not thrown when index is equal to the length of array!");
            retVal = false;
        }
        catch (ArgumentException)
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

    public bool NegTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest4: ArgumentException should be thrown when index is greater than the length of array.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            string[] TestArray = { "first", "second", "", "" };
            TestQueue.CopyTo(TestArray, 10);
            TestLibrary.TestFramework.LogError("N04.1", "ArgumentException is not thrown when index is greater than the length of array!");
            retVal = false;
        }
        catch (ArgumentException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N04.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest5: ArgumentException should be thrown when the number of elements in the source Queue is greater than the available space from index to the end of the destination array.");

        try
        {
            Queue<string> TestQueue = new Queue<string>();
            TestQueue.Enqueue("one");
            TestQueue.Enqueue("two");
            string[] TestArray = { "first", "second", "", "" };
            TestQueue.CopyTo(TestArray, 3);
            TestLibrary.TestFramework.LogError("N05.1", "ArgumentException is not thrown when the number of elements in the source Queue is greater than the available space from index to the end of the destination array!");
            retVal = false;
        }
        catch (ArgumentException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N05.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogVerbose(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
