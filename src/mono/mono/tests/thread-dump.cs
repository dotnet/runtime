using System;
using System.Reflection;
using System.Threading;

//
// This is a test program for the thread dump support in the runtime
//
// Usage:
// - start it
// - send it a SIGQUIT signal using pkill -QUIT mono
//

public class Tests {

	public static Object lock_object;

	public static void run () {
		while (true)
			;
	}

	public static void wait () {
		Monitor.Enter (lock_object);
	}

	public static void Main () {
		lock_object = new Object ();
		Monitor.Enter (lock_object);

		Thread.CurrentThread.Name = "Main";

		Thread t1 = new Thread (new ThreadStart (run));
		t1.Name = "Thread1";
		t1.Start ();
		Thread t2 = new Thread (new ThreadStart (run));
		t2.Name = "Thread2";
		t2.Start ();

		Thread t3 = new Thread (new ThreadStart (wait));
		t3.Name = "WaitThread";
		t3.Start ();

	}
}

