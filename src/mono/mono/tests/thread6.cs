using System;
using System.Threading;

public class MultiThreadExceptionTest {

	public static int result = 0;
	public static object started = new object ();
	
	public static void ThreadStart1 () {
		Console.WriteLine("{0} started", 
				  Thread.CurrentThread.Name);

		try {
			try {
				try {
					lock (started) {
						Monitor.Pulse (started);
					}
					int i = 0;
					try {
						while (true) {
							Console.WriteLine ("Count: " + i++);
							Thread.Sleep (100);
						}
					}
					catch (ThreadAbortException e) {
						Console.WriteLine ("cought exception level 3 ");

						// Check that the exception is only rethrown in
						// the appropriate catch clauses

						// This doesn't work currently, see
						// http://bugzilla.ximian.com/show_bug.cgi?id=68552

						/*
						try {
						}
						catch {}
						try {
							throw new DivideByZeroException ();
						}
						catch (Exception) {
						}
						*/
						result |= 32;

						// Check that the exception is properly rethrown
					}
					result = 255;
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
			Console.WriteLine ("cought exception level 0")
;			Console.WriteLine (e);
			Console.WriteLine (e.StackTrace);
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

	static string regress_78024 ()
	{
		try {
			Thread.CurrentThread.Abort ();
		} catch (Exception e) {
			return "Got exception: " + e.Message;
		} finally {
		}
		return "";
	}
	
	public static int Main() {
		// Check aborting the current thread
		bool aborted = false;
		try {
			Thread.CurrentThread.Abort ();
		}
		catch {
			aborted = true;
			Thread.ResetAbort ();
		}
		if (!aborted)
			return 2;

		Thread t1 = null;

		lock (started) {
			t1 = new Thread(new ThreadStart
							(MultiThreadExceptionTest.ThreadStart1));
			t1.Name = "Thread 1";

			Thread.Sleep (100);
		
			t1.Start();

			Monitor.Wait (started);
		}

		Thread.Sleep (100);

		t1.Abort ("STATETEST");

		t1.Join ();

		if (result != 59) {
			Console.WriteLine ("Result: " + result);
			return 1;
		}

		// Test from #68552
		try {
			try {
				Run ();
			} catch (Exception ex) {
			}

			return 2;
		}
		catch (ThreadAbortException ex) {
			Thread.ResetAbort ();
		}

		// Test from #78024
		try {
			regress_78024 ();
			return 3;
		}
		catch (ThreadAbortException ex) {
			Thread.ResetAbort ();
		}

		return 0;
	}

	public static void Run ()
	{
		try {
			Thread.CurrentThread.Abort ();
		} catch (Exception ex) {
			throw new Exception ("other");
		}
	}
}

