using System;
using System.Threading;

public class MultiThreadExceptionTest {

	public static int result = 0;
	
	public static void ThreadStart1 () {
		Console.WriteLine("{0} started", 
				  Thread.CurrentThread.Name);

		try {
			try {
				int i = 0;
				try {
					while (true) {
						Console.WriteLine ("Count: " + i++);
						Thread.Sleep (100);
					}
				} catch (ThreadAbortException e) {
					Console.WriteLine ("cought exception level 2 " + e.ExceptionState);
					Console.WriteLine (e);
					if ((string)e.ExceptionState == "STATETEST")
						result |= 1;
					Thread.ResetAbort ();
					throw e;
				}
			} catch (ThreadAbortException e) {
				Console.WriteLine ("cought exception level 1 " + e.ExceptionState);
				Console.WriteLine (e);
				if (e.ExceptionState == null)
					result |= 2;
			}
		} catch (Exception e) {
			Console.WriteLine ("cought exception level 0");
			Console.WriteLine (e);
			result |= 4;
		}

		try {
			Thread.ResetAbort ();
		} catch (System.Threading.ThreadStateException e) {
			result |= 8;			
		}
		
		Console.WriteLine ("end");
		result |= 16;
	}
	
	public static int Main() {
		Thread t1 = new Thread(new ThreadStart
			(MultiThreadExceptionTest.ThreadStart1));
		t1.Name = "Thread 1";

		Thread.Sleep (100);
		
		t1.Start();

		//Thread t0 = Thread.CurrentThread;
		//t0.Abort ();
		
		Thread.Sleep (200);
		t1.Abort ("STATETEST");

		t1.Join ();
		Console.WriteLine ("Result: " + result);

		if (result != 27)
			return 1;

		return 0;
	}
}

