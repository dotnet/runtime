using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;

class Test {
	public delegate int SimpleDelegate (int a, int b);

	[DllImport ("libtest.so", EntryPoint="mono_invoke_delegate")]
	static extern int mono_invoke_delegate (SimpleDelegate d);

	public static int Add (int a, int b) {
		Console.WriteLine ("Test.Add from delegate: " + a +  "+ " + b);
		return a + b;
	}

	static int Main () {
		SimpleDelegate d = new SimpleDelegate (Add);
		
		mono_invoke_delegate (d);

		return 0;
	}
}
