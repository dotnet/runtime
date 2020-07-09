// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;

public delegate string myMethodDelegate(int myInt);

class MyDelClass
{
    public string Show(int myInt)
    {
        Console.WriteLine(myInt);
        return myInt.ToString();
    }
}

class ThreadStartGen
{
    string num = string.Empty;
    static int iSet = 0;

    public static int Main(string[] args)
    {
        // check args
        if(args.Length != 1)
        {
            Console.WriteLine("USAGE: ThreadStartInt <int>|min|max\n");
            return -1;
        }

        // check for max or min
        if(args[0].ToLower() == "max")
            iSet = Int32.MaxValue;
        else if(args[0].ToLower() == "min")
            iSet = Int32.MinValue;       
        else
            iSet = Convert.ToInt32(args[0]);

        ThreadStartGen tsg = new ThreadStartGen();
        return tsg.Run();
    }

    private int Run()
    {
        MyDelClass mdc = new MyDelClass();
        myMethodDelegate md1 = new myMethodDelegate(mdc.Show);

        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(md1);
        t.Join();
        Console.WriteLine(iSet.ToString() == num ? "Test Passed" : "Test Failed");
        return (iSet.ToString() == num ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        num = ((myMethodDelegate)o)(iSet);
    }
}
