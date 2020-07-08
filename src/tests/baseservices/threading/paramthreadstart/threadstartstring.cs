// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;

class ThreadStartString
{
    string s = string.Empty;

    public static int Main(string[] args)
    {
        // check args
        if(args.Length != 1)
        {
            Console.WriteLine("USAGE: ThreadStartString <string>|null\n");
            return -1;
        }

        string sToPass = string.Empty;
        if(args[0].ToLower() == "null")
            sToPass = null;
        else
            sToPass = args[0];

        ThreadStartString tss = new ThreadStartString();
        return tss.Run(sToPass);
    }

    private int Run(string sPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(sPass);
        t.Join();
        Console.WriteLine(s == sPass ? "Test Passed" : "Test Failed");
        return (s == sPass ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        s = (string)o;
        Console.WriteLine(s);
    }
}
