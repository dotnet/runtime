
using System;
using System.Threading;

public class foo {
	public static void Main() {
		Thread thr=new Thread(new ThreadStart(foo.thread));
		thr.IsBackground=true;
		thr.Start();
		Thread.Sleep(600);
		Console.WriteLine("Aborting child thread");
		thr.Abort();
		Thread.Sleep(600);
		Console.WriteLine("Main thread returns");
	}

	public static void thread() {
		try {
			Console.WriteLine("Thread running");
			Thread.Sleep(500);
		} catch(ThreadAbortException) {
			Thread.ResetAbort();
			Console.WriteLine("Abort reset!");
		} finally {
			Console.WriteLine("ThreadAbortException finally");
		}
		try {
			Console.WriteLine("Thread running");
			Thread.Sleep(500);
		} catch(ThreadAbortException) {
			Thread.ResetAbort();
			Console.WriteLine("Abort reset!");
		} finally {
			Console.WriteLine("ThreadAbortException finally");
		}
		try {
			Console.WriteLine("Thread running");
			Thread.Sleep(500);
		} catch(ThreadAbortException) {
			Thread.ResetAbort();
			Console.WriteLine("Abort reset!");
		} finally {
			Console.WriteLine("ThreadAbortException finally");
		}
		try {
			Console.WriteLine("Thread running");
			Thread.Sleep(500);
		} catch(ThreadAbortException) {
			Thread.ResetAbort();
			Console.WriteLine("Abort reset!");
		} finally {
			Console.WriteLine("ThreadAbortException finally");
		}
		try {
			Console.WriteLine("Thread running");
		} catch(ThreadAbortException) {
			Thread.ResetAbort();
			Console.WriteLine("Abort reset!");
		} finally {
			Console.WriteLine("ThreadAbortException finally");
		}
	}
}

