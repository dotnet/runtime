
using System;
using System.Threading;

public class foo {
	public static void Main() {
		Thread thr=new Thread(new ThreadStart(foo.thread));
		thr.Start();
		Thread.Sleep(600);
		Console.WriteLine("Aborting child thread");
		thr.Abort();
		Console.WriteLine("Main thread returns");
	}

	public static void thread() {
		try {
			Console.WriteLine("Thread running 1");
			Thread.Sleep(500);
		} catch(ThreadAbortException) {
			Thread.ResetAbort();
			Console.WriteLine("Abort reset! 1");
		} finally {
			Console.WriteLine("ThreadAbortException finally 1");
		}
		try {
			Console.WriteLine("Thread running 2");
			Thread.Sleep(500);
		} catch(ThreadAbortException) {
			Thread.ResetAbort();
			Console.WriteLine("Abort reset! 2");
		} finally {
			Console.WriteLine("ThreadAbortException finally 2");
		}
		try {
			Console.WriteLine("Thread running 3");
			Thread.Sleep(500);
		} catch(ThreadAbortException) {
			Thread.ResetAbort();
			Console.WriteLine("Abort reset! 3");
		} finally {
			Console.WriteLine("ThreadAbortException finally 3");
		}
		try {
			Console.WriteLine("Thread running 4");
			Thread.Sleep(500);
		} catch(ThreadAbortException) {
			Thread.ResetAbort();
			Console.WriteLine("Abort reset! 4");
		} finally {
			Console.WriteLine("ThreadAbortException finally 4");
		}
		try {
			Console.WriteLine("Thread running 5");
		} catch(ThreadAbortException) {
			Thread.ResetAbort();
			Console.WriteLine("Abort reset! 5");
		} finally {
			Console.WriteLine("ThreadAbortException finally 5");
		}
	}
}

