using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

public class Tests  {
	static void Main ()
	{
		TestDriver.RunTests (typeof (Tests));
	}

	// Check that try-catch clauses are not enlarged to encompass a Monitor.Enter
	public static int test_0_enter_catch_clause () {
		try {
			Monitor.Enter (null);
			try {
				Console.WriteLine ();
			} catch (Exception ex) {
				return 1;
			}
		} catch (Exception ex) {
			return 0;
		}
		return 1;
	}

	const int thread_count = 3;

	// #651546
	public static int test_0_enter_abort_race () {
		AppDomain ad = AppDomain.CreateDomain ("foo");
		Thread t = new Thread (StartAppDomain);
		t.Start (ad);
		Thread.Sleep (thread_count * 100 * 2);
		// This will abort the threads created by StartAppDomain
		AppDomain.Unload (ad);
		return 0;
	}
	
	static void StartAppDomain (object dummy)
	{
		((AppDomain) dummy).DoCallBack (Main2);
	}

	static void Main2 ()
	{
		Thread[] t = new Thread [thread_count];
		for (int i = 0; i < t.Length; i++) {
			t[i] = new Thread (LockMe);
			t[i].Start (i);
			Thread.Sleep (100); // this is just so that gdb's [New Thread ...] message are properly coupled with our "Thread # entered" messages
		}
		Thread.Sleep ((int) (thread_count * 100 * 1.5));
	}

	static object the_lock = new object ();

	static void LockMe (object thread_id)
	{
		bool unlocked = false;
		try {
			Monitor.Enter (the_lock);
			try {
				Thread.Sleep (thread_count * 1000);
			} finally {
				unlocked = true;
				Monitor.Exit (the_lock);
			}
		
		} catch (Exception ex) {
			if (!unlocked) {
			}
		} finally {
		}
	}
}
