using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;

class Test {
	public delegate int SimpleDelegate (int a, int b);

	[DllImport ("libtest", EntryPoint="mono_invoke_delegate")]
	static extern int mono_invoke_delegate (SimpleDelegate d);

	public static int Add (int a, int b) {
		Console.WriteLine ("Test.Add from delegate: " + a +  " + " + b);
		return a + b;
	}

	public static int Add2 (int a, int b) {
		Console.WriteLine ("Test.Add2 from delegate: " + a +  " + " + b);
		return a + b;
	}

	static int Main () {
		SimpleDelegate d = new SimpleDelegate (Add);
		SimpleDelegate d2 = new SimpleDelegate (Add2);
		
		if (mono_invoke_delegate (d) != 5)
			return 1;

		if (mono_invoke_delegate (d2) != 5)
			return 1;

		return 0;
	}
}
