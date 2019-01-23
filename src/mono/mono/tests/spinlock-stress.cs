using System;
using System.Threading;

internal class SpinLockWrapper
{
	public SpinLock Lock = new SpinLock (false);
}

public class Tests
{
	public static void Main (string[] args)
	{
		int iterations = 200;
		if (args.Length > 0)
			iterations = Int32.Parse (args [0]);

		ParallelTestHelper.Repeat (delegate {
			int currentCount = 0;
			bool fail = false;
			SpinLockWrapper wrapper = new SpinLockWrapper ();

			ParallelTestHelper.ParallelStressTest (wrapper, delegate {
				bool taken = false;
				wrapper.Lock.Enter (ref taken);
				int current = currentCount++;
				if (current != 0)
					fail = true;

				SpinWait sw = new SpinWait ();
				for (int i = 0; i < 200; i++)
					sw.SpinOnce ();
				currentCount -= 1;

				wrapper.Lock.Exit ();
			}, 4);

			if (fail)
				Environment.Exit (1);
		}, iterations);
		Environment.Exit (0);
	}
}

static class ParallelTestHelper
{
	public static void Repeat (Action action, int numRun)
	{
		for (int i = 0; i < numRun; i++) {
			//Console.WriteLine ("Run " + i.ToString ());
			action ();
		}
	}

	public static void ParallelStressTest<TSource>(TSource obj, Action<TSource> action, int numThread)
	{
		Thread[] threads = new Thread[numThread];
		for (int i = 0; i < numThread; i++) {
			threads[i] = new Thread(new ThreadStart(delegate { action(obj); }));
			threads[i].Start();
		}

		// Wait for the completion
		for (int i = 0; i < numThread; i++)
			threads[i].Join();
	}
}
