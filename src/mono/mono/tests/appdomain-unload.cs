using System;
using System.Threading;

[Serializable]
public class Foo {

	~Foo () {
		Console.WriteLine ("FINALIZING IN DOMAIN " + AppDomain.CurrentDomain.FriendlyName + ": " + AppDomain.CurrentDomain.IsFinalizingForUnload ());
	}
}

[Serializable]
public class SlowFinalize {

	~SlowFinalize () {
		Console.WriteLine ("FINALIZE1.");
		try {
			Thread.Sleep (500);
		}
		catch (Exception ex) {
			Console.WriteLine ("A: " + ex);
		}
		Console.WriteLine ("FINALIZE2.");
	}
}

[Serializable]
public class AThread {

	public AThread () {
		new Thread (new ThreadStart (Run)).Start ();
	}

	public void Run () {
		try {
			while (true)
				Thread.Sleep (100);
		}
		catch (ThreadAbortException ex) {
			Console.WriteLine ("Thread aborted correctly.");
		}
	}
}

[Serializable]
// A Thread which refuses to die
public class BThread {

	public BThread () {
		new Thread (new ThreadStart (Run)).Start ();
	}

	public void Run () {
		try {
			while (true)
				Thread.Sleep (100);
		}
		catch (ThreadAbortException ex) {
			Thread.Sleep (1000000000);
		}
	}
}

public class UnloadThread {

	AppDomain domain;

	public UnloadThread (AppDomain domain) {
		this.domain = domain;
	}

	public void Run () {
		Console.WriteLine ("UNLOAD1");
		AppDomain.Unload (domain);
		Console.WriteLine ("UNLOAD2");
	}
}

public class Tests
{
	public static int Main() {
		return TestDriver.RunTests (typeof (Tests));
	}

	public static int test_0_unload () {
		for (int i = 0; i < 10; ++i) {
			AppDomain appDomain = AppDomain.CreateDomain("Test-unload" + i);
			AppDomain.Unload(appDomain);
		}

		return 0;
	}

	public static int test_0_unload_default () {
		try {
			AppDomain.Unload (Thread.GetDomain ());
		}
		catch (CannotUnloadAppDomainException) {
			return 0;
		}
		return 1;
	}

	public static int test_0_unload_after_unload () {
		AppDomain domain = AppDomain.CreateDomain ("Test2");
		AppDomain.Unload (domain);

		try {
			AppDomain.Unload (domain);
		}
		catch (Exception) {
			return 0;
		}

		return 1;
	}

	public static int test_0_is_finalizing () {
		AppDomain domain = AppDomain.CreateDomain ("Test-is-finalizing");
		object o = domain.CreateInstanceFromAndUnwrap (typeof (Tests).Assembly.Location, "Foo");

		if (domain.IsFinalizingForUnload ())
			return 1;

		AppDomain.Unload (domain);

		return 0;
	}

	public static int test_0_unload_with_active_threads () {
		AppDomain domain = AppDomain.CreateDomain ("Test3");
		object o = domain.CreateInstanceFromAndUnwrap (typeof (Tests).Assembly.Location, "AThread");
		Thread.Sleep (100);

		AppDomain.Unload (domain);

		return 0;
	}

	public static int test_0_unload_with_active_threads_timeout () {
		AppDomain domain = AppDomain.CreateDomain ("Test4");
		object o = domain.CreateInstanceFromAndUnwrap (typeof (Tests).Assembly.Location, "BThread");
		Thread.Sleep (100);

		try {
			AppDomain.Unload (domain);
		}
		catch (Exception) {
			return 0;
		}

		return 1;
	}

	// FIXME: This does not work yet, because the thread is finalized too
	// early
	/*
	public static int test_0_unload_during_unload () {
		AppDomain domain = AppDomain.CreateDomain ("Test3");
		object o = domain.CreateInstanceFromAndUnwrap (typeof (Tests).Assembly.Location, "SlowFinalize");

		UnloadThread t = new UnloadThread (domain);

		// Start unloading in a separate thread
		new Thread (new ThreadStart (t.Run)).Start ();

		Thread.Sleep (100);

		try {
			AppDomain.Unload (domain);
		}
		catch (Exception) {
			Console.WriteLine ("OK");
		}

		return 0;
	}	
*/
}

