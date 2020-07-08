// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//ThreadStatics are only initialized on the first thread to call the constructor
//   All other threads get the value 0 set for value types


using System;
using System.Threading;

public class MyData
{
    public AutoResetEvent autoEvent;

    [ThreadStatic]
    private static int Two = 1;

    public bool pass = false;

    public void ThreadTarget()
    {
        autoEvent.WaitOne();
        pass = CheckValues();
    }

    private bool CheckValues()
    {
        if(Two != 0)
            return false;
        return true;
    }

}

public class Test
{

    private int retVal = 0;

    public static int Main()
    {
        Test staticsTest = new Test();        
        staticsTest.RunTest();
        Console.WriteLine(100 == staticsTest.retVal ? "Test Passed":"Test Failed");
        return staticsTest.retVal;
    }

    public void RunTest()
    {
        MyData data = new MyData();
        data.autoEvent = new AutoResetEvent(true);

        //This method touches the ThreadStatic members forcing static constructors to be run
        data.ThreadTarget();
        if (data.pass != false)
        {
            Console.WriteLine("Init did not pass");
            retVal = 25;
            return;
        }        
        
        Thread t = new Thread(data.ThreadTarget);
        t.Start();
        if(!t.IsAlive)
        {
            Console.WriteLine("Thread was not set to Alive after starting");
            retVal = 50;
            return;
        }
        data.autoEvent.Set();            
        t.Join();
        if(data.pass)
            retVal = 100;
    }

}




