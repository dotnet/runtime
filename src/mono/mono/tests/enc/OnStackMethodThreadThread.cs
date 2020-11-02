using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using MonoEnc;

public class OnStackMethodThreadThread {
	public static EncHelper replacer = null;
	public static Assembly assm = null;
	public static int state = 0;
	public static Semaphore sem = null;

	public static int Main (string []args) {
		assm = typeof (OnStackMethodThreadThread).Assembly;
		replacer = EncHelper.Make ();
		sem = new Semaphore (0, 1);

		int res = DiffTestMethod1 (0);
		if (res != 1)
			return 1;

		res = DiffTestMethod1 (0);
		if (res != 2)
			return 2;

		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static int DiffTestMethod1 (int worker) {
		if (worker == 1) {
			Console.WriteLine ("Hello from Wrapper1");
			sem.WaitOne ();
			Console.WriteLine ("Hello from Wrapper1: Done waiting");
			return 0x10001;
		} else if (worker == 2) {
			throw new Exception ("should not happen");
		} else {
			Console.WriteLine ("Hello old World");
			DoTheUpdate ();
			Console.WriteLine ("Hello old World #2");
			Console.WriteLine ("Hello old World #3");
			return 1;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Wrapper1 () {
		int ret = DiffTestMethod1 (1);
		if (ret != 0x10001)
			Environment.Exit (5);
		ret = DiffTestMethod1 (1);
		if (ret != 0x10002)
			Environment.Exit (6);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Wrapper2 () {
		int ret = DiffTestMethod1 (2);
		if (ret != 0x20002)
			Environment.Exit (7);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void DoTheUpdate () {
		if (state == 0) {
			Thread thread1 = new Thread(new ThreadStart(Wrapper1));
			thread1.Start();

			replacer.Update (assm);
			state++;

			Thread thread2 = new Thread(new ThreadStart(Wrapper2));
			thread2.Start();
			thread2.Join ();

			sem.Release (1);
			thread1.Join ();
		}
	}
}

