// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class ThreadStartObject
{
    object o = null;

    public static int Main(string[] args)
    {
        // check args
        if(args.Length != 1)
        {
            Console.WriteLine("USAGE: ThreadStartObject <object>|min|max\n");
            return -1;
        }

        ThreadStartObject tso = new ThreadStartObject();
        return tso.Run(args[0]);
    }

    private int Run(object oPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(oPass);
        t.Join();
        Console.WriteLine(o == oPass ? "Test Passed" : "Test Failed");
        return (o == oPass ? 100 : -1);
    }

    private void ThreadWorker(Object oPassed)
    {
        o = oPassed;
        Console.WriteLine(o);
    }
}