// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Diagnostics;

class ThreadStartClass
{
    Object oSaved = null;

    public static int Main(string[] args)
    {
        ThreadStartClass tsc = new ThreadStartClass();
        return tsc.Run();
    }

    private int Run()
    {
    	 int iRet = 100;
        Stopwatch sw = new Stopwatch();
        sw.Start();
        Thread.Sleep(1000);
        sw.Stop();

        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(sw);
        t.Join();

	 if (sw.ElapsedMilliseconds != ((Stopwatch)oSaved).ElapsedMilliseconds)
	 {
	 	Console.WriteLine("Expected ((Stopwatch)oSaved).ElapsedMilliseconds = sw.ElapsedMilliseconds=" + sw.ElapsedMilliseconds.ToString());
	 	Console.WriteLine("Actual oSaved.ElapsedMilliseconds=" + ((Stopwatch)oSaved).ElapsedMilliseconds.ToString());
	 	iRet = 98;
	 }
	         	Console.WriteLine("Expected ((Stopwatch)oSaved).ElapsedMilliseconds to be 1000, but found " + ((Stopwatch)oSaved).ElapsedMilliseconds);
        if (((Stopwatch)oSaved).ElapsedMilliseconds < 950)
        {
        	Console.WriteLine("Expected ((Stopwatch)oSaved).ElapsedMilliseconds to be 1000, but found " + ((Stopwatch)oSaved).ElapsedMilliseconds);
		iRet = 97;
        }
	
        Console.WriteLine(iRet==100 ? "Test Passed" : "Test Failed");
        return iRet;
    }

    private void ThreadWorker(Object o)
    {
        oSaved = o;
    }
}
