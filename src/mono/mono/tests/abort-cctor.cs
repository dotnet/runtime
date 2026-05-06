using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.CompilerServices;

class Driver
{
	public static SemaphoreSlim sema1 = new SemaphoreSlim (0);
	public static ManualResetEvent mre1 = new ManualResetEvent (false);
	public static ManualResetEvent mre2 = new ManualResetEvent (false);

	class StaticConstructor1
	{
		internal static bool gotToEnd, caughtException;
		static StaticConstructor1 ()
		{
			try {
				Console.WriteLine ("StaticConstructor1.StaticConstructor1 (1)");
				Driver.mre1.Set ();
				var sw = Stopwatch.StartNew ();
				Thread.Sleep (1000);
				sw.Stop ();
				typeof (string).GetMethods ();
				//XXX we assume that if we slept less than 900ms we got aborted
				if (sw.ElapsedMilliseconds < 900)
					throw new Exception ("Bad abort broke our sleep");
				Console.WriteLine ("StaticConstructor1.StaticConstructor1 (2) waited {0}", sw.ElapsedMilliseconds);
				gotToEnd = true;
			} catch (Exception e) {
				caughtException = true;
				throw;
			}
		}

		public static void Init ()
		{
			Console.WriteLine ("StaticConstructor1.Init");
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void IsStaticConstructor1Viable () {
		new StaticConstructor1 ();
		Console.WriteLine ("Did it get to the end? {0} Did it catch an exception {1}", StaticConstructor1.gotToEnd, StaticConstructor1.caughtException);
		if (!StaticConstructor1.gotToEnd) /* the TAE must not land during a .cctor */
			Environment.Exit (1);
		if (StaticConstructor1.caughtException)
			Environment.Exit (2);

	}

	static void Test1 ()
	{
		Console.WriteLine ("Test 1:");

		Driver.mre1.Reset ();
		Driver.mre2.Reset ();

		Thread thread = new Thread (() => {
			try {
				StaticConstructor1.Init ();
			} catch (Exception e) {
				Console.WriteLine ("StaticConstructor1::init caught exception {0}", e);

				if (!(e is ThreadAbortException))
					throw;
			}
		});

		thread.Start ();

		Driver.mre1.WaitOne ();

		// The ThreadAbortException should land while in
		// the StaticConstructor1.cctor. The exception should
		// be queued, and be rethrown when exiting the cctor.
		thread.Abort ();

		thread.Join ();

		//is StaticConstructor1 viable?
		try {
			IsStaticConstructor1Viable ();
			Console.WriteLine ("StaticConstructor1 is viable"); /* a TAE doesn't make a type unusable */
		} catch (TypeInitializationException  e) {
			Console.WriteLine ("StaticConstructor1 not viable");
			Environment.Exit (3);
		}
	}

	class StaticConstructor2Exception : Exception {}

	class StaticConstructor2
	{
		static StaticConstructor2 ()
		{
			Console.WriteLine ("StaticConstructor2.StaticConstructor2 (1)");
			Driver.mre1.Set ();
			throw new StaticConstructor2Exception ();
			/* Unreachable */
			Driver.mre2.Set ();
			Console.WriteLine ("StaticConstructor2.StaticConstructor2 (2)");
		}

		public static void Init ()
		{
			Console.WriteLine ("StaticConstructor2.Init");
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void IsStaticConstructor2Viable () {
		new StaticConstructor2 ();
	}


	static void Test2 ()
	{
		Console.WriteLine ("Test 2:");

		Driver.mre1.Reset ();
		Driver.mre2.Reset ();

		Thread thread = new Thread (() => {
			try {
				StaticConstructor2.Init ();
			} catch (TypeInitializationException e) {
				Console.WriteLine (e);

				if (!(e.InnerException is StaticConstructor2Exception))
					throw;
			}
		});

		thread.Start ();

		Driver.mre1.WaitOne ();

		// A InvalidOperationException should be thrown while in
		// the StaticConstructor2.cctor. The exception should
		// be wrapped in a TypeInitializationException.

		if (Driver.mre2.WaitOne (500)) {
			/* We shouldn't reach Driver.mre.Set () in StaticConstructor2.cctor */
			Environment.Exit (4);
		}

		thread.Join ();

		//is StaticConstructor2 viable?
		try {
			IsStaticConstructor2Viable ();
			Console.WriteLine ("StaticConstructor2 is viable");
			/* A regular exception escaping the .cctor makes the type not usable */
			Environment.Exit (5);
		} catch (TypeInitializationException e) {
			Console.WriteLine ("StaticConstructor2 not viable");
		}

	}

	class StaticConstructor3
	{
		static StaticConstructor3 ()
		{
			Console.WriteLine ("StaticConstructor3.StaticConstructor3 (1)");
			Driver.mre1.Set ();
			Thread.CurrentThread.Abort ();
			/* Unreachable */
			Driver.mre2.Set ();
			Console.WriteLine ("StaticConstructor3.StaticConstructor3 (2)");
			Environment.Exit (6);
		}

		public static void Init ()
		{
			Console.WriteLine ("StaticConstructor3.Init");
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void IsStaticConstructor3Viable () {
		new StaticConstructor3 ();
	}

	static void Test3 ()
	{
		Console.WriteLine ("Test 3:");
		bool caught_abort = false;

		Driver.mre1.Reset ();
		Driver.mre2.Reset ();

		Thread thread = new Thread (() => {
			try {
				StaticConstructor3.Init ();
				Console.WriteLine ("cctor3 didn't throw?!?!");
				/* StaticConstructor3 self aborted */
				Environment.Exit (7);
			} catch (ThreadAbortException e) {
				Console.WriteLine ("TEST 3: aborted {0}", e);
				caught_abort = true;
			}
		});

		thread.Start ();

		Driver.mre1.WaitOne ();

		// A InvalidOperationException should be thrown while in
		// the StaticConstructor2.cctor. The exception should
		// be wrapped in a TypeInitializationException.

		thread.Join ();

		// Did we catch the abort
		if (!caught_abort)
			Environment.Exit (8);

		//is StaticConstructor2 viable?
		try {
			IsStaticConstructor3Viable ();
			Console.WriteLine ("StaticConstructor3 is viable");
			/* A regular exception escaping the .cctor makes the type not usable */
			Environment.Exit (9);
		} catch (TypeInitializationException e) {
			Console.WriteLine ("StaticConstructor3 not viable");
		}
	}





	class StaticConstructor4
	{
		internal static bool gotToEnd, caughtException;

		static StaticConstructor4 ()
		{
			try {
				Console.WriteLine ("StaticConstructor4.StaticConstructor4 (1)");
				Driver.mre1.Set ();
				var sw = Stopwatch.StartNew ();
				Thread.Sleep (1000);
				sw.Stop ();
				typeof (string).GetMethods ();
				//XXX we assume that if we slept less than 900ms we got aborted
				if (sw.ElapsedMilliseconds < 900)
					throw new Exception ("Bad abort broke our sleep");
				Console.WriteLine ("StaticConstructor4.StaticConstructor4 (2) waited {0}", sw.ElapsedMilliseconds);
				gotToEnd = true;
			} catch (Exception e) {
				caughtException = true;
				throw;
			}
		}

		public static void Init ()
		{
			Console.WriteLine ("StaticConstructor4.Init");
		}
	}

	static bool got_to_the_end_of_the_finally = false;

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void IsStaticConstructor4Viable () {
		new StaticConstructor4 ();
		Console.WriteLine ("IsStaticConstructor4Viable: Did it get to the end? {0} Did it catch an exception {1} and end of the finally block {2}", StaticConstructor4.gotToEnd, StaticConstructor4.caughtException, got_to_the_end_of_the_finally);
		if (!StaticConstructor4.gotToEnd) /* the TAE must not land during a .cctor */
			Environment.Exit (10);
		if (StaticConstructor4.caughtException)
			Environment.Exit (11);
	}

	static void Test4 ()
	{
		Console.WriteLine ("Test 4:");

		Driver.mre1.Reset ();
		Driver.mre2.Reset ();

		Thread thread = new Thread (() => {
			try {

				try {
				} finally {
					StaticConstructor4.Init ();
					Console.WriteLine ("Test 4: After the cctor");
					got_to_the_end_of_the_finally = true;
				}
			} catch (Exception e) {
				Console.WriteLine ("StaticConstructor4::init caught exception {0}", e);
				if (!(e is ThreadAbortException))
					throw;
				if (!got_to_the_end_of_the_finally)
					throw new Exception ("Test 4: did not get to the end of the cctor");
			}
		});

		thread.Start ();

		Driver.mre1.WaitOne ();

		// The ThreadAbortException should land while in
		// the StaticConstructor4.cctor. The exception should
		// be queued, and be rethrown when exiting the cctor.
		thread.Abort ();

		thread.Join ();

		if (!got_to_the_end_of_the_finally) {
			Console.WriteLine ("Did not get to the end of test 4 cctor");
			Environment.Exit (12);
		}

		//is StaticConstructor4viable?
		try {
			IsStaticConstructor4Viable ();
			Console.WriteLine ("StaticConstructor4 is viable"); /* a TAE doesn't make a type unusable */
		} catch (TypeInitializationException  e) {
			Console.WriteLine ("StaticConstructor4 not viable");
			Environment.Exit (13);
		}
	}


	class StaticConstructor5 {
		public static bool caught_exception = false;
		static StaticConstructor5 ()
		{
			Driver.mre1.Set ();
			Driver.mre2.WaitOne ();
			try {
				throw new Exception ();
			} catch (Exception) {
				Console.WriteLine ("Caught exception in cctor");
				caught_exception = true;
			}
		}
	}

	static void Test5 ()
	{
		bool caught_abort = false;
		Driver.mre1.Reset ();
		Driver.mre2.Reset ();
		Thread thread = new Thread (() => {
					try {
						new StaticConstructor5 ();
					} catch (ThreadAbortException) {
						Console.WriteLine ("Caught thread abort");
						caught_abort = true;
					}
				});
		thread.Start ();

		Driver.mre1.WaitOne ();
		thread.Abort ();
		Driver.mre2.Set ();

		thread.Join ();

		if (!StaticConstructor5.caught_exception)
			Environment.Exit (14);
		if (!caught_abort)
			Environment.Exit (15);
	}

	public static bool got_to_the_end_of_outer_finally = false;
	public static bool got_to_the_end_of_inner_finally = false;

	class StaticConstructor6 {

		static StaticConstructor6 ()
		{
			try {
				Setup6 ();
			} finally {
				Driver.got_to_the_end_of_outer_finally = true;
			}
		}

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public static void Setup6 () {
			try {
			} finally {
				Driver.sema1.Release ();
				Thread.Sleep (1000); /* hopefully we get woken up here */
				Driver.got_to_the_end_of_inner_finally = true;
			}
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void IsStaticConstructor6Viable () {
		Console.WriteLine ("IsStaticConstructor6Viable? got to inner finally? {0} got to outer finally? {1}",
				   Driver.got_to_the_end_of_inner_finally,
				   Driver.got_to_the_end_of_outer_finally);
		new StaticConstructor6 ();
		if (!Driver.got_to_the_end_of_inner_finally)
			Environment.Exit (17);
		if (!Driver.got_to_the_end_of_outer_finally)
			Environment.Exit (18);
	}

	public static void Test6 ()
	{
		Thread thread = new Thread (() => {
				new StaticConstructor6 ();
			});

		thread.Start ();
		Driver.sema1.Wait ();
		thread.Abort ();
		thread.Join ();
		try {
			IsStaticConstructor6Viable ();
			Console.WriteLine ("StaticConstructor6 is viable");
		} catch (TypeInitializationException e) {
			Console.WriteLine ("StaticConstructor6 is not viable");
			Environment.Exit (19);
		}
	}

	public static int Main ()
	{
		Test1 ();
		Test2 ();
		Test3 ();
		Test4 ();
		Test5 ();
		Test6 ();
		Console.WriteLine ("done, all things good");
		return 0;
	}
}
