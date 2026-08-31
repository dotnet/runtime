
using System;
using System.Threading;

public class Test {
	private void Thread_func() {
		Console.WriteLine("In a thread!");
		
		Thread thr=Thread.CurrentThread;
		
		Console.WriteLine("Locking thr for 1.5s");
		lock(thr) {
			Console.WriteLine("Locked");
			Thread.Sleep(2000);
			Console.WriteLine("Slept for 2s");
			Thread.Sleep(15000);
		}
		
		Console.WriteLine("Waiting for signal");
		lock(thr) {
			Console.WriteLine("Waiting...");
			Monitor.Wait(thr);
			Console.WriteLine("Thread signalled!");
		}
		
		Console.WriteLine("Sleeping for 2s");
		Thread.Sleep(2000);

		Console.WriteLine("Leaving thread");
	}
	
	public static int Main () {
		Console.WriteLine ("Hello, World!");
		Thread thr=new Thread(new ThreadStart(new Test().Thread_func));
		thr.Start();
		Thread.Sleep(1000);

		Console.WriteLine("Trying to enter lock");
		if(Monitor.TryEnter(thr, 1000)==true) {
			Console.WriteLine("Returned lock");
			Monitor.Exit(thr);
		} else {
			Console.WriteLine("Didn't get lock");
			// .net seems to leave thr locked here !!!!
			
			// This test deadlocks on .net with the thread
			// trying to lock(thr) between the two
			// WriteLine()s Monitor.Exit(thr); here and it
			// magically works :) Of course, then mint
			// throws a
			// SynchronizationLockException... (like it
			// should)
			//Monitor.Exit(thr);
		}
		
		Thread.Sleep(20000);

		lock(thr) {
			Monitor.Pulse(thr);
			Console.WriteLine("Signalled thread");
		}
		
		thr.Join();
		
		return 0;
	}
}

