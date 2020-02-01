using System;
using System.Threading;
using System.Runtime.InteropServices;

class Test {
	delegate int SimpleDelegate (int a);

	static int F (int a) {
		Console.WriteLine ("Test.F from delegate: " + a);
		Thread.Sleep (200);
		return a;
	}

	static void async_callback (IAsyncResult ar)
	{
		Console.WriteLine ("Async Callback " + ar.AsyncState);
	}
	
	static int Main () {
		SimpleDelegate d = new SimpleDelegate (F);
		AsyncCallback ac = new AsyncCallback (async_callback);
		string state1 = "STATE1";
		string state2 = "STATE2";
		string state3 = "STATE3";
		string state4 = "STATE4";
		int fin = 0;
		
		IAsyncResult ar1 = d.BeginInvoke (1, ac, state1);
		IAsyncResult ar2 = d.BeginInvoke (2, ac, state2);
		IAsyncResult ar3 = d.BeginInvoke (3, ac, state3);
		IAsyncResult ar4 = d.BeginInvoke (4, ac, state4);
		
		int res = d.EndInvoke (ar1);

		Console.WriteLine ("Result = " + res);

		try {
			d.EndInvoke (ar1);
		} catch (InvalidOperationException) {
			Console.WriteLine ("cant execute EndInvoke twice ... OK");
		}

		ar1.AsyncWaitHandle.WaitOne ();
		if (ar1.IsCompleted) fin++;
		Console.WriteLine ("completed1: " + ar1.IsCompleted);
		ar2.AsyncWaitHandle.WaitOne ();
		if (ar2.IsCompleted) fin++;
		Console.WriteLine ("completed2: " + ar2.IsCompleted);
		ar3.AsyncWaitHandle.WaitOne ();
		if (ar3.IsCompleted) fin++;
		Console.WriteLine ("completed3: " + ar3.IsCompleted);
		ar4.AsyncWaitHandle.WaitOne ();		
		if (ar4.IsCompleted) fin++;
		Console.WriteLine ("completed4: " + ar4.IsCompleted);

		if (fin != 4)
			return 1;
		
		return 0;
	}
}
