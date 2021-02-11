using System;
using System.Threading;
using System.Runtime.InteropServices;

class Test {
	delegate int SimpleDelegate (int a);

	static int cb_state = 0;
	
	static int F (int a) {
		Console.WriteLine ("Test.F from delegate: " + a);
		throw new NotImplementedException ();
	}

	static void async_callback (IAsyncResult ar)
	{
		Console.WriteLine ("Async Callback " + ar.AsyncState);
		cb_state = 1;
		throw new NotImplementedException ();
	}
	
	static int Main () {
		SimpleDelegate d = new SimpleDelegate (F);
		AsyncCallback ac = new AsyncCallback (async_callback);
		string state1 = "STATE1";
		int res = 0;
		
		IAsyncResult ar1 = d.BeginInvoke (1, ac, state1);

		ar1.AsyncWaitHandle.WaitOne ();

		try {
			res = d.EndInvoke (ar1);
		} catch (NotImplementedException) {
			res = 1;
			Console.WriteLine ("received exception ... OK");
		}

		while (cb_state == 0)
			Thread.Sleep (0);
		
		if (cb_state != 1)
			return 1;
		
		if (res != 1)
			return 2;

		return 0;
	}
}
