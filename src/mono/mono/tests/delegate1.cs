using System;
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
		
		d.EndInvoke (ar);
		
		return 0;
	}
}
}
