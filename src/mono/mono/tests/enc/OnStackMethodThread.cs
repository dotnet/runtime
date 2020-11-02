using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using MonoEnc;

public class OnStackMethod {
	public static EncHelper replacer = null;
	public static Assembly assm = null;
	public static int state = 0;

	public static int Main (string []args) {
		assm = typeof (OnStackMethod).Assembly;
		replacer = EncHelper.Make ();

		int res = DiffTestMethod1 ();
		if (res != 1)
			return 1;

		res = DiffTestMethod1 ();
		if (res != 2)
			return 2;

		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static int DiffTestMethod1 () {
		Console.WriteLine ("Hello old World");
		DoTheUpdate ();
		Console.WriteLine ("Hello old World #2");
		Console.WriteLine ("Hello old World #3");
		return 1;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Wrapper () {
		int ret = DiffTestMethod1 ();
		if (ret != 2)
			Environment.Exit (5);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void DoTheUpdate () {
		if (state == 0) {
			replacer.Update (assm);
			state++;
			
			Thread thread = new Thread(new ThreadStart(Wrapper));
			thread.Start();
			thread.Join ();
		}
	}
}

