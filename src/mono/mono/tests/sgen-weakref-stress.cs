using System;
using System.Threading;

public class Tests
{
	const int thread_count = 10;
	const int weakrefs_per_thread = 5000;
	const int crash_loops = 5;
	public static void CrashRound () {
		var t = new Thread [thread_count];
		int fcount = 0;
		for (int i = 0; i < thread_count; ++i) {
			t [i] = new Thread (delegate () {
			   for (int j = 0; j < weakrefs_per_thread; ++j) {
			       new WeakReference (new object ());
			   }
			   Interlocked.Increment (ref fcount);
			});
		}

		for (int i = 0; i < thread_count; ++i)
			t [i].Start ();

		while (true) {
			if (fcount == thread_count)
				break;
			GC.Collect ();
			Thread.Sleep (1);
		}
	}
	
	public static void Main () {
		for (int i = 0; i < crash_loops; ++i) {
			Console.WriteLine ("{0}", i);
			CrashRound ();
		}
	}
}