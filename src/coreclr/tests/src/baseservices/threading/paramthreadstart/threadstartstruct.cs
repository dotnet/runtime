// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

class ThreadStartStruct
{
    int iRet = -1;

    public struct MyStruct
    {
        public int myInt;
        public string myStr;  
        public long myLong;  
        public double myDouble;  
        public Mutex myMutex;
    }

    public static int Main()
    {
        ThreadStartStruct tss = new ThreadStartStruct();
        return tss.Run();
    }

    private int Run()
    {
        MyStruct m = new MyStruct();
        m.myInt = Int32.MinValue;
        m.myStr = "This is the string";
        m.myLong = Int64.MaxValue;
        m.myDouble = Double.MinValue;
        m.myMutex = new Mutex(true);

        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(m);
        // Test to see if it passing an owned mutex
        Thread.Sleep(1000);
        m.myMutex.ReleaseMutex();
        t.Join();

        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return (100 == iRet ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        if(Int32.MinValue == ((MyStruct)o).myInt &&
            "This is the string" == ((MyStruct)o).myStr &&
            Int64.MaxValue == ((MyStruct)o).myLong &&
            Double.MinValue == ((MyStruct)o).myDouble)
        {
            try
            {
                ((MyStruct)o).myMutex.WaitOne();
                ((MyStruct)o).myMutex.ReleaseMutex();
                iRet = 100;
            }
            catch(Exception e)
            {
                Console.WriteLine("Unexpected exception thrown: " + 
                    e.ToString());
            }
        }
    }
}
