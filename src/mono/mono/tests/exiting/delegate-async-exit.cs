using System;
using System.Threading;
using System.Runtime.InteropServices;

class foo {
	delegate void foo_delegate ();
	
	static void function () {
		Console.WriteLine ("Delegate method");
	}

	static void async_callback (IAsyncResult ar)
	{
		Console.WriteLine ("Async callback " + ar.AsyncState);
		Environment.Exit(0);
	}
	
	public static int Main () {
		Environment.ExitCode = 2;
		foo_delegate d = new foo_delegate (function);
		AsyncCallback ac = new AsyncCallback (async_callback);
		IAsyncResult ar1 = d.BeginInvoke (ac, "foo");

		ar1.AsyncWaitHandle.WaitOne();
		d.EndInvoke(ar1);

		Thread.Sleep(1000);
		Console.WriteLine("Main returns");
		return 1;
	}
}
