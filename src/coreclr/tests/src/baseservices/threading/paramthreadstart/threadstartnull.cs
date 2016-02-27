// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class ThreadStartNull
{
    int iRet = -1;

    public static int Main()
    {
        ThreadStartNull tsn = new ThreadStartNull();
        return tsn.Run();
    }

    private int Run()
    {
        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(null);
        t.Join();
        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }

    private void ThreadWorker(Object o)
    {
        if(null == o)
            iRet = 100;
        else
            Console.WriteLine("Object was not null");
    }
}