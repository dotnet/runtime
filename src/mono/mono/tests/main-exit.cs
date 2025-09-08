
using System;
using System.Threading;

public class foo {
	public static int Main() {
		Environment.ExitCode = 2;
		Thread thr=new Thread(new ThreadStart(foo.thread));
		thr.Start();
		Thread.Sleep(1200);
		Console.WriteLine("Main thread exiting");
		Environment.Exit(0);
		return 1;
	}

	public static void thread() {
		Console.WriteLine("Thread running");
		Thread.Sleep(500);
		Console.WriteLine("Thread running");
		Thread.Sleep(500);
		Console.WriteLine("Thread running");
		Thread.Sleep(500);
		Console.WriteLine("Thread running");
		Thread.Sleep(500);
		Console.WriteLine("Thread running");
	}
}

