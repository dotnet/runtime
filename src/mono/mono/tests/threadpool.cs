using System;
using System.Threading;

public class ThreadPoolTest
{

	static int csum = 0;
	
	public static void test_callback (object state) {
		Console.WriteLine("test_callback:" + state);
		Thread.Sleep (200);
		Interlocked.Increment (ref csum);
	}
	
	public static int Main () {
		int workerThreads;
		int completionPortThreads;
		
		ThreadPool.GetMaxThreads (out workerThreads, out completionPortThreads);
		Console.WriteLine ("workerThreads: {0} completionPortThreads: {1}", workerThreads, completionPortThreads);
		
		ThreadPool.GetAvailableThreads (out workerThreads, out completionPortThreads);
		Console.WriteLine ("workerThreads: {0} completionPortThreads: {1}", workerThreads, completionPortThreads);

		ThreadPool.QueueUserWorkItem (new WaitCallback (test_callback), "TEST1");
		ThreadPool.QueueUserWorkItem (new WaitCallback (test_callback), "TEST2");
		ThreadPool.QueueUserWorkItem (new WaitCallback (test_callback), "TEST3");
		ThreadPool.QueueUserWorkItem (new WaitCallback (test_callback), "TEST4");
		ThreadPool.QueueUserWorkItem (new WaitCallback (test_callback), "TEST5");
		ThreadPool.QueueUserWorkItem (new WaitCallback (test_callback));

		while (csum < 6) {
			Thread.Sleep (100);
		}

		Console.WriteLine ("CSUM: " + csum);

		if (csum != 6)
			return 1;

		return 0;
	}
}

