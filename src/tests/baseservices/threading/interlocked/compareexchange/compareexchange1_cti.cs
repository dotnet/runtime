// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

/// <summary>
/// System.Threading.Interlocked.CompareExchange(Double,Double,Double)
/// </summary>
/// 

// This test makes sure that CompareExchange(Double, Double, Double)
// plays nicely with another thread accessing shared state directly
public class InterlockedCompareExchange1
{
    public static double globalValue = 0.0;
    public Thread threadA;
    public Thread threadB;
    public double state;


    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Using another thread to change the value of variable in this thread");


        try
        {
            // Spin up two new threads
            threadA = new Thread(new ThreadStart(TestComChange));
            threadB = new Thread(new ThreadStart(changeGlobal));
            // Start Thread A
            // Thread A runs TestComChange
            threadA.Start();
            // Block calling thread until spawned Thread A completes
            threadA.Join();
            // now, the final value of globalValue and state should be -0.1
            if (globalValue != -0.1 && state != -0.1)
            {
                TestLibrary.TestFramework.LogError("001", "The method did not works, the result is" + globalValue + " " + state);
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
    #endregion

    #region Negetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ");

        try
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        InterlockedCompareExchange1 test = new InterlockedCompareExchange1();

        TestLibrary.TestFramework.BeginTestCase("InterlockedCompareExchange1");

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
    public void TestComChange()
    {
        // loop 20 times.  On the 0-9th times, 
        // only Thread A is accessing globalValue. it keeps 
        // checking if globalValue is 10, which it isn't (it 
        // starts at 0).  but, on the 10th time, it starts 
        // Thread B, which at some point runs changeGlobal, 
        // setting globalValue to 10.  After Thread B sets 
        // it to 10, the CompareExchange will fire, setting 
        // globalValue to -0.1.  Thereafter, CompareExchange 
        // will see that globalValue is not 10, and keep 
        // returning -0.1 to state. each time thru the loop,
        // Thread A does a sleep(10) so that Thread B has a 
        // chance to do its thing.
        int i = 0;
        while (i < 20)
        {
            if (i == 10)
            {
                threadB.Start();
                threadB.Join();
            }
            state = Interlocked.CompareExchange(ref globalValue, -0.1, 10.0);
            i++;
        }
    }
    public void changeGlobal()
    {
        // Thread B is the only place this runs
        // When it runs, it simply sets globalValue to 10.
        // It should only do this once Thread A tells Thread B 
        // to start.
        globalValue = 10.0;
    }
}
