using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.IO;

/*
TODO
	Some tests are much more expensive than others (on cycles and synchronization), add a calibration step to figure out duration.
	Some tests are more disrruptive than others, add weights to tests so some are picked more frequently.
	The workload is too static, add some background generation noise tests.
	The workload is too stable, add perturbance on the number of available tasks.
	Fuse threadpool with non threadpool workloads by firing some tasks as separate threads.
	Add an external watchdog so we can run the stress test in a loop for very long times and get results out of it.
	We don't have enough tests, add one per locking operation we got in the runtime.

Missing tests:
	Ephemerons
	Dynamic methods
	Regular SRE
	Remoting / Transparent Proxies
	Context locals
	Thread locals
	Finalizers
	Async socket IO
*/

class Driver {
	static bool stop_please;
	const int TEST_DURATION = 1000;

	static object very_contended_object = new object ();

	static bool Ok (ref int loops) {
		if (loops == -1)
			return !stop_please;
		if (loops == 0)
			return false;
		loops--;
		return true;

	}
	static void MonitorEnterInALoop (int loops)
	{
		while (Ok (ref loops)) {
			if (Monitor.TryEnter (very_contended_object, 100)) {
				Thread.Sleep (30);
				Monitor.Exit (very_contended_object);
			}
		}
	}

	static void AllocObjectInALoop (int loops) {
		while (Ok (ref loops)) {
			var a = new object ();
			var b = new byte [100];
		}
	}

	static void AllocDomainInALoop (int loops) {
		int count = 0;
		while (Ok (ref loops)) {
			var a = AppDomain.CreateDomain ("test_domain_" + ++count);
			AppDomain.Unload (a);
		}
	}

	static void FileIO (int loops) {
		while (Ok (ref loops)) {
			var dir = Path.GetTempFileName () + "_" + Thread.CurrentThread.ManagedThreadId;
			Directory.CreateDirectory (dir);
			Directory.Delete (dir);
			
		}
	}

    static void Timer_Elapsed(object sender, EventArgs e)
    {
        HashSet<string> h = new HashSet<string>();
        for (int j = 0; j < 500; j++)
        {
            h.Add(""+j+""+j);
        }
    }

	//From sgen-new-threads-dont-join-stw
	static void TimerStress (int loops) {
		while (Ok (ref loops)) {
			System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = false;
            timer.Interval = 500;
            timer.Start ();
		}
	}

	//from sgen-weakref-stress
	static void WeakRefStress (int loops) {
		while (Ok (ref loops)) {
		   for (int j = 0; j < 500; ++j) {
		       new WeakReference (new object ());
		   }
		}
	}
	static Tuple<Action<int>,string>[] available_tests = new [] {
		Tuple.Create (new Action<int> (MonitorEnterInALoop), "monitor"),
		Tuple.Create (new Action<int> (AllocObjectInALoop), "alloc"),
		Tuple.Create (new Action<int> (AllocDomainInALoop), "appdomain"),
		Tuple.Create (new Action<int> (FileIO), "file-io"),
		Tuple.Create (new Action<int> (TimerStress), "timers"),
		Tuple.Create (new Action<int> (WeakRefStress), "weakref"),
	};

	static void GcPump (int timeInMillis)
	{
		var sw = Stopwatch.StartNew ();
		do {
			GC.Collect ();
			Thread.Sleep (1);
		} while (sw.ElapsedMilliseconds < timeInMillis);
		stop_please = true;
	}

	const int minTpSteps = 1;
	const int maxTpSteps = 30;

	static void QueueStuffUsingTpl (int threadCount) {
		int pendingJobs = 0;
		int maxPending = threadCount * 2;
		int generatorIdx = 0;
		Random rand = new Random (0);

		while (!stop_please) {
			while (pendingJobs < maxPending) {
				var task = available_tests [generatorIdx++ % available_tests.Length].Item1;
				int steps = rand.Next(minTpSteps, maxTpSteps);
				ThreadPool.QueueUserWorkItem (_ => {
					task (steps);
					Interlocked.Decrement (ref pendingJobs);
				});
				Interlocked.Increment (ref pendingJobs);
			}
			Thread.Sleep (1);
		}
		while (pendingJobs > 0)
			Thread.Sleep (1);
	}

	static void DynamicLoadGenerator (int threadCount, int timeInMillis) {
		var t = new Thread (() => QueueStuffUsingTpl (threadCount));
		t.Start ();

		GcPump (timeInMillis);

		t.Join ();
	}

	static void StaticLoadGenerator (int threadCount, int testIndex, int timeInMillis) {
		List<Thread> threads = new List<Thread> ();

		for (int i = 0; i < threadCount; ++i) {
			var dele = (testIndex >= 0 ? available_tests [testIndex] : available_tests [i % available_tests.Length]).Item1;
			var t = new Thread (() => dele (-1));
			t.Start ();
			threads.Add (t);
		}

		GcPump (timeInMillis);

		foreach (var t in threads)
			t.Join ();
	}
	
	static int ParseTestName (string name) {
		for (int i = 0; i < available_tests.Length; ++i) {
			if (available_tests[i].Item2 == name)
				return i;
		}
		Console.WriteLine ("Invalid test name {0}", name);
		Environment.Exit (2);
		return -1;
	}

	static int Main (string[] args) {
		int threadCount = Environment.ProcessorCount - 1;
		int timeInMillis = TEST_DURATION;
		int testIndex = -1;
		bool tpLoadGenerator = false;
		string testName = "static";
		

		for (int j = 0; j < args.Length;) {
			if ((args [j] == "--duration") || (args [j] == "-d")) {
				timeInMillis = Int32.Parse (args [j + 1]);
				j += 2;
			} else if ((args [j] == "--test") || (args [j] == "-t")) {
				if (args [j + 1] == "static")
					testIndex = -1;
				else if (args [j + 1] == "tp")
					tpLoadGenerator = true;
				else
					testIndex = ParseTestName (testName = args [j + 1]);
				j += 2;
			} else 	if ((args [j] == "--thread-count") || (args [j] == "-tc")) {
				threadCount = Int32.Parse (args [j + 1]);
				j += 2;
			}else {
				Console.WriteLine ("Unknown argument: " + args [j]);
				return 1;
			}
        }

		if (tpLoadGenerator) {
			Console.WriteLine ("tp window {0} duration {1}", threadCount, timeInMillis);
			DynamicLoadGenerator (threadCount, timeInMillis);
		} else {
			Console.WriteLine ("thread count {0} duration {1} test {2}", threadCount, timeInMillis, testName);
			StaticLoadGenerator (threadCount, testIndex, timeInMillis);
		}

		return 0;
	}
}