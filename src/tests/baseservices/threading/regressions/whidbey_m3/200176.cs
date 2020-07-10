// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

public class Stop {

    public static int Main(String[] args) {              

        Stop tm = new Stop();
	try
	{

		ThreadPool.QueueUserWorkItem(new WaitCallback(tm.RunTest));
		Thread.Sleep(3000);
	}
	catch
	{
		return -1;
	}
	return 100;
    }
    public void RunTest(object foo)
    {
        try{
	    throw new Exception();
        }
        catch
        {}        
    }
}
