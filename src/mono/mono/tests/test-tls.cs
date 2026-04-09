using System;
using System.Threading;

public class Program
{
	public const int nr_threads = 4;
	public const int reps = 10000;
	public static int[] allocs = new int[nr_threads];

	public Program (int index)
	{
		allocs [index] += 1;
	}

	public static void Work (object oindex)
	{
		int index = (int)oindex;
		for (int i = 0; i < reps; i++) {
			Thread thread = Thread.CurrentThread;
			if (string.Compare (thread.Name, "t" + index) == 0)
				new Program (index);
		}
	}

	public static int Main (string[] args)
	{
		Thread[] threads = new Thread[nr_threads];

		for (int i = 0; i < nr_threads; i++) {
			threads [i] = new Thread (Work);
			threads [i].Name = "t" + i;
			threads [i].Start (i);
		}

		for (int i = 0; i < nr_threads; i++) {
			threads [i].Join ();
			if (allocs [i] != reps)
				return 1;
		}

		return 0;
	}
}
