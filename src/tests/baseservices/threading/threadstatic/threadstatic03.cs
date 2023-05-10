// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//ThreadStatics are only initialized on the first thread to call the constructor
//   All other threads get the value 0 set for value types


using System;
using System.Threading;
using Xunit;

public class MyData
{
    public AutoResetEvent autoEvent;

    [ThreadStatic]
    private static int Two = 1;

    //This static constructor causes the C# compiler to make this class precise instead of beforefieldinit
    static MyData()
    {
    }

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

public class Test_threadstatic03
{

    private int retVal = 0;

    [Fact]
    public static int TestEntryPoint()
    {
        Test_threadstatic03 staticsTest = new Test_threadstatic03();        
        staticsTest.RunTest();
        Console.WriteLine(100 == staticsTest.retVal ? "Test Passed":"Test Failed");
        return staticsTest.retVal;
    }

    public void RunTest()
    {
        MyData data = new MyData();
        data.autoEvent = new AutoResetEvent(false);        
        
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




