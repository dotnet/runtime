using System;
using System.Threading;

class Driver
{
	static ManualResetEvent mre = new ManualResetEvent (false);

	class MyClassInThread
	{
		public int I { get; set; }

		~MyClassInThread()
		{
			try {
				Console.WriteLine($"Finalizer {I}");
			} catch (NotSupportedException) {
			}

			mre.Set ();
		}
	}

	public static void Main(string[] args)
	{
		for (int i = 0; i < 50; ++i) {
			SpawnThread(i);
			GC.Collect();
			GC.Collect();
			GC.Collect();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			Console.WriteLine($"Loop      {i}");
		}

		if (!mre.WaitOne(0))
			Environment.Exit (1);
	}

	static void SpawnThread(int i)
	{
		var th = new Thread(_ => {}) { IsBackground = true, };
		th.Start(new MyClassInThread { I = i });
		th.Join();
	}
}
