// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class MyGenClass<ItemType>
{
    public void Show(ItemType myType, out ItemType outType)
    {
        Console.WriteLine(myType);
        outType = myType;
    }
}

class ThreadStartGen
{
    int iNum = 0;
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
        MyGenClass<int> myObj1 = new MyGenClass<int>();

        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(myObj1);
        t.Join();
        Console.WriteLine(iSet == iNum ? "Test Passed" : "Test Failed");
        return (iSet == iNum ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        ((MyGenClass<int>)o).Show(iSet, out iNum);
    }
}