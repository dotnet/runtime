
using System;
using System.Threading;

public class foo {
	public static void Main() {
		// it will actually return 0 only if the thread finishes executing
		Environment.ExitCode = 1;
		Thread thr=new Thread(new ThreadStart(foo.thread));
		thr.Start();
		Thread.Sleep(1200);
		Console.WriteLine("Main thread returns");
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
		Environment.ExitCode = 0;
	}
}

