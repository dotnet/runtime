// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class ThreadStartBool
{
    bool b = false;

    public static int Main(string[] args)
    {
        // check args
        if(args.Length != 1)
        {
            Console.WriteLine("USAGE: ThreadStartBool <bool>\n");
            return -1;
        }

        ThreadStartBool tsb = new ThreadStartBool();
        return tsb.Run(Convert.ToBoolean(args[0]));
    }

    private int Run(bool bPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(bPass);
        t.Join();
        Console.WriteLine(b == bPass ? "Test Passed" : "Test Failed");
        return (b == bPass ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        b = (bool)o;
        Console.WriteLine(b);
    }
}