using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;

class Test {
	delegate int SimpleDelegate (int a);

	static int cb_state = 0;
	
	static int F (int a) {
		Console.WriteLine ("Test.F from delegate: " + a);
		throw new NotImplementedException ();
	}

	static void async_callback (IAsyncResult ar)
	{
		AsyncResult ares = (AsyncResult)ar;
		AsyncCallback ac = new AsyncCallback (async_callback);
		
		Console.WriteLine ("Async Callback " + ar.AsyncState);
		cb_state++;
		SimpleDelegate d = (SimpleDelegate)ares.AsyncDelegate;

		if (cb_state < 5)
			d.BeginInvoke (cb_state, ac, cb_state);
		
		//throw new NotImplementedException ();
	}
	
	static int Main () {
		SimpleDelegate d = new SimpleDelegate (F);
		AsyncCallback ac = new AsyncCallback (async_callback);
		
		IAsyncResult ar1 = d.BeginInvoke (cb_state, ac, cb_state);

		ar1.AsyncWaitHandle.WaitOne ();


		while (cb_state < 5)
			Thread.Sleep (200);

		return 0;
	}
}
