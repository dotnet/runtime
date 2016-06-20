using System;
using System.Threading;
using System.Runtime;
using System.Text;

public class Tests
{
	public static int Main ()
	{
		return TestDriver.RunTests (typeof (Tests));
	}

	public static void TestMethod()
	{
		Console.WriteLine("{0} with {1} priority",
			Thread.CurrentThread.Name, 
			Thread.CurrentThread.Priority.ToString());
		Thread.Sleep(6000);
		Console.WriteLine("{0} with {1} priority",
			Thread.CurrentThread.Name, 
			Thread.CurrentThread.Priority.ToString());
	}
	
	public static int test_0_thread_priority () 
	{
		int res = 0;

		Thread Me = Thread.CurrentThread;
		Thread TestThread = new Thread(new ThreadStart(TestMethod));

		Console.WriteLine("Starting test thread with priority to AboveNormal");	 
		ThreadPriority before = TestThread.Priority;
		TestThread.Priority = ThreadPriority.AboveNormal;
		TestThread.Name = "TestMethod";
		TestThread.Start();
		ThreadPriority after = TestThread.Priority;
		Console.WriteLine("Priority: {0} {1}",before,after);
		if (before != ThreadPriority.Normal)
			res = 1;
		else if (after != ThreadPriority.AboveNormal)
			res = 2;
		else {
			TestThread.Priority = ThreadPriority.Normal;
			after = TestThread.Priority;
			Console.WriteLine("Setting test thread priority to Normal");	 
			Thread.Sleep(1000);
			Console.WriteLine("Priority: {0} {1}",before,after);

			if (after != ThreadPriority.Normal) 
				res = 3;
			else {
				Console.WriteLine("Setting test thread priority to AboveNormal");	 
				before = after;
				TestThread.Priority=ThreadPriority.AboveNormal;
				after = TestThread.Priority;
				Thread.Sleep(1000);
				Console.WriteLine("Priority: {0} {1}",before,after);

				if (after != ThreadPriority.AboveNormal) 
					res = 4;
				else {
					before = after;
					Console.WriteLine("Setting test thread priority to BelowNormal"); 
					TestThread.Priority=ThreadPriority.BelowNormal;
					after = TestThread.Priority;
					Console.WriteLine("Priority: {0} {1}",before,after);
					Thread.Sleep(1000);
					
					if (after != ThreadPriority.BelowNormal)
						res = 5;
					else {
						before = after;
						Console.WriteLine("Setting test thread priority back to Normal");	 
						TestThread.Priority=ThreadPriority.Normal;
						after = TestThread.Priority;
						Console.WriteLine("Priority: {0} {1}",before,after);
						Thread.Sleep(1000);

						if (after != ThreadPriority.Normal)
							res = 6;
					}
				}
			}
		}
		TestThread.Join();
		return(res);
	}
}
