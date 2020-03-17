using System;
using System.Threading;
using System.Runtime.InteropServices;

class AsyncException : Exception {}

class Tests
{
	delegate int SimpleDelegate (int a);

	static int cb_state = 0;

	static int async_func (int a)
	{
		Console.WriteLine ("async_func from delegate: " + a);
		return 10;
	}

	static int async_func_throws (int a)
	{
		Console.WriteLine ("async_func_throws from delegate: " + a);
		throw new AsyncException ();
	}

	static void async_callback (IAsyncResult ar)
	{
		Console.WriteLine ("Async Callback " + ar.AsyncState);
		cb_state = 1;
	}

	static int Main ()
	{
		SimpleDelegate d = new SimpleDelegate (async_func_throws);
		AsyncCallback ac = new AsyncCallback (async_callback);
		string state1 = "STATE1";

		// Call delegate via ThreadPool and check that the exception is rethrown correctly
		IAsyncResult ar1 = d.BeginInvoke (1, ac, state1);

		while (cb_state == 0)
			Thread.Sleep (0);

		try {
			d.EndInvoke (ar1);
			Console.WriteLine ("NO EXCEPTION");
			return 1;
		} catch (AsyncException) {
			Console.WriteLine ("received exception ... OK");
			return 0;
		} catch (Exception e) {
			Console.WriteLine ("wrong exception {0}", e);
			return 3;
		}
	}
}
