using System;
using System.Threading;

public class Test {

	static int csum = 0;
	
	public static void test_callback (object state) {
		int workerThreads;
		int completionPortThreads;
		ThreadPool.GetAvailableThreads (out workerThreads, out completionPortThreads);
		Console.WriteLine("test_casllback:" + state + "ATH: " + workerThreads);
		Thread.Sleep (10);

		Interlocked.Increment (ref csum);
	}
	
	public static int Main () {
		int workerThreads;
		int completionPortThreads;
		int runs = 10;
		
		ThreadPool.GetMaxThreads (out workerThreads, out completionPortThreads);
		Console.WriteLine ("workerThreads: {0} completionPortThreads: {1}", workerThreads, completionPortThreads);

		ThreadPool.GetAvailableThreads (out workerThreads, out completionPortThreads);
		Console.WriteLine ("workerThreads: {0} completionPortThreads: {1}", workerThreads, completionPortThreads);

		for (int i = 0; i < runs; i++) {
			ThreadPool.QueueUserWorkItem (new WaitCallback (test_callback), "TEST1 " + i);
			ThreadPool.QueueUserWorkItem (new WaitCallback (test_callback), "TEST2 " + i);
			ThreadPool.QueueUserWorkItem (new WaitCallback (test_callback), "TEST3 " + i);
			ThreadPool.QueueUserWorkItem (new WaitCallback (test_callback), "TEST4 " + i);
			ThreadPool.QueueUserWorkItem (new WaitCallback (test_callback), "TEST5 " + i);

			do {
				ThreadPool.GetAvailableThreads (out workerThreads, out completionPortThreads);
				if (workerThreads == 0)
					Thread.Sleep (100);
			} while (workerThreads == 0);
			

			ThreadPool.GetAvailableThreads (out workerThreads, out completionPortThreads);
			Console.WriteLine ("workerThreads: {0} completionPortThreads: {1}", workerThreads, completionPortThreads);
		}

		while (csum < (runs * 5)) {
			Thread.Sleep (100);

		}
		
		Console.WriteLine ("CSUM: " + csum);

		if (csum != (runs * 5))
			return 1;
		
		return 0;
	}
}

