
using System;
using System.Threading;

public class foo {
	public static int Main() {
		Thread thr=new Thread(new ThreadStart(foo.thread));
		thr.Start();
		Thread.Sleep(1200);
		Console.WriteLine("Main thread returns");
		// the subthread calls Exit(0) before we reach here
		return 1;
	}

	public static void thread() {
		Console.WriteLine("Thread running");
		Thread.Sleep(500);
		Console.WriteLine("Thread exiting");
		Environment.Exit(0);
	}
}

