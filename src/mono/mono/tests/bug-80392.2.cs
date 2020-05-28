using System;
using System.Threading;
using System.Runtime.InteropServices;

class Test {
	delegate int SimpleDelegate (int a);

	static int Method (int a) {
		return a;
	}

	static void Callback (IAsyncResult ar)
	{
	}
	
	static int Main () {
		SimpleDelegate d1 = new SimpleDelegate (Method);
		SimpleDelegate d2 = new SimpleDelegate (Method);

		AsyncCallback ac = new AsyncCallback (Callback);
		
		IAsyncResult ar1 = d1.BeginInvoke (1, ac, null);
		IAsyncResult ar2 = d2.BeginInvoke (2, ac, null);

		try {
			d1.EndInvoke (ar2);
			return 1;
		} catch (InvalidOperationException) {
			// expected
		}

		try {
			d2.EndInvoke (ar1);
			return 2;
		} catch (InvalidOperationException) {
			// expected
		}

		if (1 != d1.EndInvoke (ar1)) return 3;
		if (2 != d2.EndInvoke (ar2)) return 4;

		return 0;
	}
}
