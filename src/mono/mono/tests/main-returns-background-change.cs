
using System;
using System.Threading;

public class foo {
	public static void Main() {
		Thread thr=new Thread(new ThreadStart(foo.thread));
		thr.Start();
		Console.WriteLine("Main thread returns");
	}

	public static void thread() {
		Console.WriteLine("Thread running");
		Thread.Sleep(500);
		Console.WriteLine("Thread running");
		Thread.Sleep(500);
		Thread.CurrentThread.IsBackground = true;
		Console.WriteLine("Thread running");
		Thread.Sleep(500);
		Console.WriteLine("Thread running");
		Thread.Sleep(500);
		Console.WriteLine("Thread running");
		Environment.Exit (1);
	}
}

