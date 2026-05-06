using System;
using System.Threading;

public class Critical {
	static Critical ()
	{
		Program.mre1.Set ();
		Program.mre2.WaitOne ();
		try {
			throw new Exception ();
		} catch (Exception) {
			Console.WriteLine ("Caught exception in cctor");
			Program.caught_exception = true;
		}
	}
}


public class Program {
	public static ManualResetEvent mre1 = new ManualResetEvent (false);
	public static ManualResetEvent mre2 = new ManualResetEvent (false);

	public static bool caught_exception, caught_abort;

	public static int Main (string[] args)
	{
		Thread thread = new Thread (DoStuff);
		thread.Start ();

		mre1.WaitOne ();
		thread.Abort ();
		mre2.Set ();

		thread.Join ();

		if (!caught_exception)
			Environment.Exit (1);
		if (!caught_abort)
			Environment.Exit (2);

		Console.WriteLine ("done, all things good");
		return 0;
	}

	public static void DoStuff ()
	{
		try {
			new Critical ();
		} catch (ThreadAbortException) {
			Console.WriteLine ("Caught thread abort");
			Program.caught_abort = true;
		}
	}
}
