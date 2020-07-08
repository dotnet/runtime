// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

/// <summary>
/// System.Threading.Interlocked.CompareExchange(ref object,object,object)
/// </summary>

// Tests that CompareExchange(object, object, object)
// plays nicely with another thread accessing shared state directly
// Also includes a test for when location = comparand = null (should 
// switch).
public class InterlockedCompareExchange5
{
    public static object globalValue;
    public Thread threadA;
    public Thread threadB;
    public object state;
    public myClass obMyClass;

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: The object is a string");

        try
        {
            // Spin up two new threads
            threadA = new Thread(new ThreadStart(TestComChange));
            threadB = new Thread(new ThreadStart(changeGlobal));
            // Start Thread A
            // Thread A runs TestComChange
            threadA.Start();
            // Block spawning thread until Thread A completes
            threadA.Join();
            // now, the final values of
            // globalValue and state should be "changedValue"
            if (globalValue.ToString() != "changedValue" && state != (object)("changedValue"))
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

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: The object is a custom class");

        obMyClass = new myClass(123456789);
        try
        {
            // Spin up two new threads
            threadA = new Thread(new ThreadStart(TestComChange2));
            threadB = new Thread(new ThreadStart(changeGlobal2));
            // Start Thread A
            // Thread A runs TestComChange2
            threadA.Start();
            // Block spawning thread until Thread A completes
            threadA.Join();
            // now, the final values of
            // globalValue and state should NOT be -100
            if (((myClass)globalValue).a != -100 && ((myClass)state).a != -100)
            {
                TestLibrary.TestFramework.LogError("003", "The method did not works, the result is" + globalValue + " " + state);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: The first argument is a null reference");

        try
        {
            // a non-null object
            object value = new myClass(-100);
            // a null object
            object comparand = null;
            // a null initial state
            globalValue = null;
            // globalValue is null, so it should switch
            // and return null
            // this is a major difference with
            // InterlockedCompareExchange8.cs --
            // here we use the object overload
            state = Interlocked.CompareExchange(ref globalValue, value, comparand);
            // globalValue should equal value now
            if (globalValue != value)
            {
                TestLibrary.TestFramework.LogError("005", "The method did not works, the result is" + globalValue + " " + state);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #region Negative Test Cases
    #endregion
    #endregion

    public static int Main()
    {
        InterlockedCompareExchange5 test = new InterlockedCompareExchange5();

        TestLibrary.TestFramework.BeginTestCase("InterlockedCompareExchange5");

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
        // set a value
        object value = "changedValue";
        // set a different value
        object comparand = "comparand";
        int i = 0;
        // loop 20 times
        while (i < 20)
        {
            // first ten times, we just skip this
            // then on the tenth time, fire Thread B,
            // setting globalValue to "Comparand"
            if (i == 10)
            {
                threadB.Start();
                threadB.Join();
            }
            // first ten iterations, globalValue does not
            // equal comparand, so it keeps returning 
            // the contents of globalValue without
            // poking value into it
            // after ten, Thread B kicks in, and
            // it matches, subsequently, globalValue
            // gets set to "changedValue"
            // this is a major difference with
            // InterlockedCompareExchange8.cs --
            // here we use the object overload
            state = Interlocked.CompareExchange(ref globalValue, value, comparand);
            i++;
        }
    }
    public void changeGlobal()
    {
        // set when B runs
        globalValue = "comparand";
    }

    public void TestComChange2()
    {
        // set a value
        object value = new myClass(-100);
        // set a different value
        object comparand = obMyClass;
        int i = 0;
        // loop 20 times
        while (i < 20)
        {
            // first ten times, we just skip this
            // then on the tenth time, fire Thread B,
            // setting globalValue to obMyClass
            if (i == 10)
            {
                threadB.Start();
                threadB.Join();
            }
            // first ten iterations, globalValue does not
            // equal comparand, so it keeps returning 
            // the contents of globalValue without
            // poking value into it
            // after ten, Thread B kicks in, and
            // it matches, subsequently, globalValue
            // gets set to point to where value does
            // this is a major difference with
            // InterlockedCompareExchange8.cs --
            // here we use the object overload
            state = Interlocked.CompareExchange(ref globalValue, value, comparand);
            i++;
        }
    }
    public void changeGlobal2()
    {
        globalValue = obMyClass;
    }
}
public class myClass
{
    public int a;
    public myClass(int value)
    {
        a = value;
    }
}
