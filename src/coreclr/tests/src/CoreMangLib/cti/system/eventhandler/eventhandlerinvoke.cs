// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

public static class Counter
{
    public static int count = 0;
}

public class MyClass
{
    public void Test1(object sender, EventArgs e)
    {
        Counter.count++;
    }

    public static void Test2(object sender, EventArgs e)
    {
        Counter.count++;
    }

    private void Test3(object sender, EventArgs e)
    {
        Console.WriteLine("Static EventArgs is {0}: ", e.ToString());
    }

    public int Test4(object sender, EventArgs e)
    {
        Console.WriteLine("Int value return EventArgs is {0}: ", e.ToString());
        return 1;
    }
}

public class EventHandlerInvoke
{
    public event EventHandler myHandler;
    
    public static int Main(string[] args)
    {
        EventHandlerInvoke invoke =
            new EventHandlerInvoke();
        TestLibrary.TestFramework.BeginTestCase("Testing EventHandler.Invoke(System.Object,System.EventArgs,System.AsyncCallback,System.Object)...");

        if (invoke.RunTests())
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
        retVal = PosTest3() && retVal;

        return retVal;
    }

    public bool PosTest1() 
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify invoke eventhandler using non-static method...");

        try
        {
            EventHandlerInvoke delctor = new EventHandlerInvoke();
            delctor.myHandler = new EventHandler(new MyClass().Test1);
            EventArgs e = new EventArgs();
            int i = Counter.count;
            delctor.myHandler.Invoke(null, e);

            if (i!=Counter.count-1)
            {
                TestLibrary.TestFramework.LogError("001", "Invoke method is not invoked!");
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
        TestLibrary.TestFramework.BeginScenario("Verify invoke eventhandler using static method...");

        try
        {
            EventHandlerInvoke delctor = new EventHandlerInvoke();
            delctor.myHandler = new EventHandler(new MyClass().Test1);
            EventArgs e = new EventArgs();
            int i = Counter.count;
            delctor.myHandler.Invoke(new MyClass(), e);

            if (i != Counter.count - 1)
            {
                TestLibrary.TestFramework.LogError("001", "Invoke method is not invoked!");
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

    public bool PosTest3() 
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify invoke eventhandler using non-static method when target object is null...");

        try
        {
            EventHandlerInvoke delctor = new EventHandlerInvoke();
            delctor.myHandler = new EventHandler(new MyClass().Test1);
            EventArgs e = new EventArgs();
            int i = Counter.count;
            delctor.myHandler.Invoke(null, e);

            if (i != Counter.count - 1)
            {
                TestLibrary.TestFramework.LogError("001", "Invoke method is not invoked!");
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

    public bool PosTest4() 
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify invoke eventhandler using static method when target object is null...");

        try
        {
            EventHandlerInvoke delctor = new EventHandlerInvoke();
            delctor.myHandler = new EventHandler(new MyClass().Test1);
            EventArgs e = new EventArgs();
            int i = Counter.count;
            delctor.myHandler.Invoke(new MyClass(), e);

            if (i != Counter.count - 1)
            {
                TestLibrary.TestFramework.LogError("001", "Invoke method is not invoked!");
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
}
