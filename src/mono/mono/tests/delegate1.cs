using System;
using System.Threading;
using System.Runtime.InteropServices;

namespace Bah {
class Test {
	delegate int SimpleDelegate (int a);

	static int F (int a) {
		Console.WriteLine ("Test.F from delegate: " + a);
		return a;
	}

	static void async_callback (IAsyncResult ar)
	{
		Console.WriteLine ("Async Callback");
	}
	
	static int Main () {
		SimpleDelegate d = new SimpleDelegate (F);
		AsyncCallback ac = new AsyncCallback (async_callback);
		string state = "STATE OBJECT";
		
		IAsyncResult ar = d.BeginInvoke (3, ac, state);
		
		int res = d.EndInvoke (ar);

		Console.WriteLine ("Result = " + res);

		try {
			d.EndInvoke (ar);
		} catch (InvalidOperationException) {
			Console.WriteLine ("cant execute EndInvoke twice ... OK");
		}

		ar.AsyncWaitHandle.WaitOne ();
		
		Console.WriteLine ("completed: " + ar.IsCompleted);
				
		return 0;
	}
}
}
