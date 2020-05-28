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
	}
	
	public static void Main () {
		foo_delegate d = new foo_delegate (function);
		AsyncCallback ac = new AsyncCallback (async_callback);
		IAsyncResult ar1 = d.BeginInvoke (ac, "foo");

		Console.WriteLine("Waiting");
		ar1.AsyncWaitHandle.WaitOne();
		Console.WriteLine("Sleeping");
		Thread.Sleep(1000);
		Console.WriteLine("EndInvoke");
		d.EndInvoke(ar1);
		Console.WriteLine("Sleeping");

		Thread.Sleep(1000);
		Console.WriteLine("Main returns");
	}
}
