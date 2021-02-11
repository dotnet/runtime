using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Runtime.CompilerServices;


class Driver {
	static int result;
	static bool finally_done;
	static ManualResetEvent handle;
	static Thread thread;
	static object broken;

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void ThrowE () {
		broken.ToString ();		
	}

	static bool InterruptRequested () {
		return (Thread.CurrentThread.ThreadState & ThreadState.AbortRequested) == ThreadState.AbortRequested;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void SleepLoop () {
		for (int i = 0; i < 10; ++i) {
			Console.WriteLine ("step {0} - {1}", i, Thread.CurrentThread.ThreadState);
			if (InterruptRequested ())
				break;
			Thread.Sleep (100);
		}

		if (!InterruptRequested ())
			result |= 0x1;

		try {
			ThrowE ();
		} catch (Exception e) {
			Console.WriteLine ("caught/0 {0} from inside the prot block", e.GetType ());
			if (!(e is NullReferenceException))
				result |= 0x2;
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void CancelAbort () {
		object lk = new object ();
		Console.WriteLine ("step 0 - {0}", Thread.CurrentThread.ThreadState);
		//lock (lk) { Monitor.Wait (lk, 100); }
		Console.WriteLine ("step 1 - {0}", Thread.CurrentThread.ThreadState);
		Thread.ResetAbort ();
	}

	/////////////////////////////////////////////////////
	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void InnerFromEH0 () {
		thread = Thread.CurrentThread;
		MethodInfo mi = typeof (Driver).GetMethod ("SleepLoop");
		try {
			try {
				throw new ArgumentException ();
			} finally {	
				handle.Set ();
				SleepLoop ();
				Console.WriteLine ("done");
				finally_done = true;
			}
			Console.WriteLine ("After finally");
			result |= 0x10;
		} catch (Exception e) {
			if (!(e is ArgumentException))
				result |= 0x4;
			Console.WriteLine ("caught/1 a {0} while on {1} res {2}", e.GetType (), Thread.CurrentThread.ThreadState, result);
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void GuardFromEH0 () {
		try {
			InnerFromEH0 ();
		} catch (Exception e) {
			if (!(e is ThreadAbortException))
				result |= 0x8;
			Console.WriteLine ("caught/2 a {0} while on {1} res {2}", e.GetType (), Thread.CurrentThread.ThreadState, result);
		}
	}


	public static int test_0_abort_finally_after_throw () {
		finally_done = false;
		result = 0;
		Action ac = GuardFromEH0;
		handle = new ManualResetEvent (false);
		var res = ac.BeginInvoke (null, null);
		handle.WaitOne ();
		Console.WriteLine ("aborting");
		thread.Abort ();
		Console.WriteLine ("aborted");
		res.AsyncWaitHandle.WaitOne ();
		Console.WriteLine ("waited");
		if (!finally_done)
			result |= 0x100;
		return result;
	}

	/////////////////////////////////////////////////////

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void InnerFromEH1 () {
		thread = Thread.CurrentThread;
		MethodInfo mi = typeof (Driver).GetMethod ("SleepLoop");
		try {
			try {
				throw new ArgumentException ();
			} finally {	
				handle.Set ();
				SleepLoop ();
				CancelAbort ();
				Console.WriteLine ("done");
				finally_done = true;
			}
			Console.WriteLine ("After finally");
			result |= 0x10;
		} catch (Exception e) {
			if (!(e is ArgumentException))
				result |= 0x4;
			Console.WriteLine ("caught/3 a {0} while on {1} res {2}", e.GetType (), Thread.CurrentThread.ThreadState, result);
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void GuardFromEH1 () {
		try {
			InnerFromEH1 ();
		} catch (Exception e) {
			result |= 0x8;
			Console.WriteLine ("caught/4 a {0} while on {1}", e.GetType (), Thread.CurrentThread.ThreadState);
		}
	}

	public static int test_0_abort_finally_and_cancel () {
		finally_done = false;
		result = 0;
		Action ac = GuardFromEH1;
		handle = new ManualResetEvent (false);
		var res = ac.BeginInvoke (null, null);
		handle.WaitOne ();
		Console.WriteLine ("aborting");
		thread.Abort ();
		Console.WriteLine ("aborted");
		res.AsyncWaitHandle.WaitOne ();
		Console.WriteLine ("waited");
		if (!finally_done)
			result |= 0x100;
		return result;
	}

	/////////////////////////////////////////////////////

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void InnerFromEH () {
		thread = Thread.CurrentThread;
		MethodInfo mi = typeof (Driver).GetMethod ("SleepLoop");
		try {
			try {
				Console.WriteLine ("try block");
			} finally {	
				handle.Set ();
				SleepLoop ();
				Console.WriteLine ("done");
				finally_done = true;
			}
			Console.WriteLine ("After finally");
			result |= 0x10;
		} catch (Exception e) {
			if (!(e is ThreadAbortException))
				result |= 0x4;
			Console.WriteLine ("caught/5 a {0} while on {1} res {2}", e.GetType (), Thread.CurrentThread.ThreadState, result);
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void GuardFromEH () {
		try {
			InnerFromEH ();
		} catch (Exception e) {
			if (!(e is ThreadAbortException))
				result |= 0x8;
			Console.WriteLine ("caught/6 a {0} while on {1} res {2}", e.GetType (), Thread.CurrentThread.ThreadState, result);
		}
	}


	public static int test_0_finally_after_try () {
		AppDomain.CurrentDomain.UnhandledException += (obj, sender) => {
			Console.WriteLine ("Unhandled {0}",  sender.ExceptionObject);
		};

		finally_done = false;
		result = 0;
		Action ac = GuardFromEH;
		handle = new ManualResetEvent (false);
		var res = ac.BeginInvoke (null, null);
		handle.WaitOne ();
		Console.WriteLine ("aborting");
		thread.Abort ();
		Console.WriteLine ("aborted");
		res.AsyncWaitHandle.WaitOne ();
		Console.WriteLine ("waited");
		if (!finally_done)
			result |= 0x100;
		return result;
	}
	/////////////////////////////////////////////////////

	static int Main (string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += (obj, sender) => {
			Console.WriteLine ("Unhandled {0}",  sender.ExceptionObject);
		};

		return TestDriver.RunTests (typeof (Driver), args);
	}
}


