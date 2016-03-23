// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

// This is a non-9x test.

class WaitAnyEx
{
	WaitHandle[] wh;
	ManualResetEvent mre = new ManualResetEvent(false);

	public static int Main()
	{
		WaitAnyEx wae = new WaitAnyEx();
		return wae.Run();
	}

	private int Run()
	{
		int iRet = -1;
		Console.WriteLine("Abandoning only one Mutex in array with other WaitHandles, signaling other mutexes");
		CreateArray(64);
		Thread t = new Thread(new ThreadStart(this.WaitOnAllExceptPos));
		t.Start();

		Thread t2 = new Thread(new ThreadStart(this.AbandonLastMutex));
	       t2.Start();

		mre.WaitOne();
		mre.Reset();
	       t2.Join();	// make sure the thread has exited before checking for the abandoned mutex

	       int i = -1;
	       try
	       {
			Console.WriteLine("Waiting...");
			i = WaitHandle.WaitAny(wh, 5000);
			Console.WriteLine("WaitAny did not throw AbandonedMutexExcpetion");
			Console.WriteLine("Object at position {0} ({1}) returned", i, wh[i].GetType());
       	}
	       catch(AbandonedMutexException am)
	       {
			Console.WriteLine("AbandonedMutexException thrown!  Checking values...");			
			if(61 == am.MutexIndex)
			{
				if (am.Mutex == wh[am.MutexIndex])
	                		iRet = 100;
				else
					Console.WriteLine("Expected AbandonedMutexException to == Indexed Mutex, but it does not.");
			}
	     		else
		 		Console.WriteLine("Expected return of 61, but found " + am.MutexIndex);
		}
		catch(Exception e)
		{
			Console.WriteLine("Unexpected exception thrown: " + e.ToString());
        	}
        	Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        	return iRet;
	}

	private void WaitOnAllExceptPos()
	{
		Mutex m = new Mutex();
		bool bSet = false;
		for(int i=wh.Length-1;i>=0;i--)
		{
			if(wh[i].GetType() == m.GetType())
			{
				if(bSet)
				{
					wh[i].WaitOne();
					Console.Write(i + ".");
                		}
                		bSet = true;
			}
		}
        	mre.Set();
	 	//This Sleep keeps Thread alive to hold Mutexes
        	Thread.Sleep(15000);
    	}

	private void AbandonLastMutex()
	{
		Mutex m = new Mutex();
		for(int i=wh.Length-1;i>=0;i--)
		{
			if(wh[i].GetType() == m.GetType())
			{
				wh[i].WaitOne();
				Console.WriteLine(i + "AbandonLastMutex.");				
                		break;
            		}
        	}
	}

	private void CreateArray(int numElements)
	{
		wh = new WaitHandle[numElements];
		for(int i=0;i<numElements;i++)
		{
			switch(i%3)
			{
				case 0:
					wh[i] = new ManualResetEvent(false);
					break;
				case 1:
					wh[i] = new Mutex(false, Common.GetUniqueName());
					break;
				case 2:
					wh[i] = new Semaphore(0,5);
					break;
			}
		}
	}
}
