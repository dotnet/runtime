
using System;
using System.Collections.Concurrent;
using System.Threading;

class Driver
{
	static TestTimeout timeout;

	public static void Main ()
	{
		int gcCount = 0;
		int joinCount = 0;
		TestTimeout timeout = TestTimeout.Start(TimeSpan.FromSeconds(TestTimeout.IsStressTest ? 60 : 5));

		Thread gcThread = new Thread (() => {
			while (timeout.HaveTimeLeft) {
				GC.Collect ();
				gcCount++;
				Thread.Sleep (1);
			}
		});

		gcThread.Start ();

		// Create threads then join them for 1 seconds (120 for stress tests) nonstop while GCs occur once per ms
		while (timeout.HaveTimeLeft) {
			BlockingCollection<Thread> threads = new BlockingCollection<Thread> (new ConcurrentQueue<Thread> (), 128);

			Thread joinThread = new Thread (() => {
				for (int i = 0; ; ++i) {
					Thread t = threads.Take ();
					if (t == null)
						break;
					t.Join ();

					// Uncomment this and run with MONO_LOG_LEVEL=info MONO_LOG_MASK=gc
					// to see GC/join balance in real time
					//Console.Write ("*");
				}
			});
			joinThread.Start ();
			
			const int makeThreads = 10*1000;
			for (int i = 0; i < makeThreads; ++i) {
				Thread t = new Thread (() => { Thread.Yield (); });
				t.Start ();

				threads.Add (t);
			}

			threads.Add (null);
			joinThread.Join ();

			joinCount += makeThreads;
			Console.WriteLine("Performed {0} GCs, created {1} threads. Finished? {2}", gcCount, joinCount, !timeout.HaveTimeLeft);
		}
		gcThread.Join ();
	}
}
