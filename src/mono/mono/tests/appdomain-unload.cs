using System;
using System.Threading;
using System.Reflection;
using System.Runtime.Remoting;

[Serializable]
public class Foo {

	~Foo () {
		Console.WriteLine ("FINALIZING IN DOMAIN " + AppDomain.CurrentDomain.FriendlyName + ": " + AppDomain.CurrentDomain.IsFinalizingForUnload ());
	}
}

public class Bar : MarshalByRefObject {
	public int test (int x) {
		Console.WriteLine ("in " + Thread.GetDomain ().FriendlyName);
		return x + 1;
	}

	public void start_wait () {
		Action a = delegate () {
			Thread.Sleep (10000);
		};
		a.BeginInvoke (null, null);
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

// A Thread which refuses to die
public class BThread : MarshalByRefObject {

	bool stop;

	public BThread () {
		new Thread (new ThreadStart (Run)).Start ();
	}

	public void Stop () {
		stop = true;
	}

	public void Run () {
		try {
			while (true)
				Thread.Sleep (100);
		}
		catch (ThreadAbortException ex) {
			while (!stop)
				Thread.Sleep (100);
		}
	}
}

public interface IRunnable {
	void Run ();
}

public class MBRObject : MarshalByRefObject, IRunnable {
	/* XDomain wrappers for invocation */
	public void Run () {
		while (true) {
			try {
				while (true)
					Thread.Sleep (100);
			}
			catch (ThreadAbortException ex) {
				Thread.ResetAbort ();
			}
		}
	}
}

public class CBObject : ContextBoundObject, IRunnable {
	/* Slow corlib path for invocation */
	public void Run () {
		while (true) {
			try {
				while (true)
					Thread.Sleep (100);
			}
			catch (ThreadAbortException ex) {
				Thread.ResetAbort ();
			}
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

class CrossDomainTester : MarshalByRefObject
{
}

public class Tests
{
	public static int Main(string[] args) {
		if (args.Length == 0)
			return TestDriver.RunTests (typeof (Tests), new String[] { "-v" });
		else
			return TestDriver.RunTests (typeof (Tests), args);
	}

	public static int test_0_unload () {
		for (int i = 0; i < 10; ++i) {
			AppDomain appDomain = AppDomain.CreateDomain("Test-unload" + i);

			appDomain.CreateInstanceAndUnwrap (
				typeof (CrossDomainTester).Assembly.FullName, "CrossDomainTester");

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

	/* In recent mono versions, there is no unload timeout */
	/*
	public static int test_0_unload_with_active_threads_timeout () {
		AppDomain domain = AppDomain.CreateDomain ("Test4");
		BThread o = (BThread)domain.CreateInstanceFromAndUnwrap (typeof (Tests).Assembly.Location, "BThread");
		Thread.Sleep (100);

		try {
			AppDomain.Unload (domain);
		}
		catch (Exception) {
			// Try again
			o.Stop ();
			AppDomain.Unload (domain);
			return 0;
		}

		return 1;
	}
	*/

	public static void ThreadStart (object obj)
	{
		IRunnable runnable = (IRunnable)obj;

		try {
			runnable.Run ();
		} catch (AppDomainUnloadedException) {
			Console.WriteLine ("OK");
		} catch (ThreadAbortException) {
			throw new Exception ();
		}
	}

	public static int test_0_unload_reset_abort () {
		AppDomain domain = AppDomain.CreateDomain ("test_0_unload_reset_abort");
		MBRObject mbro = (MBRObject) domain.CreateInstanceFromAndUnwrap (typeof (Tests).Assembly.Location, "MBRObject");
		CBObject cbo = (CBObject) domain.CreateInstanceFromAndUnwrap (typeof (Tests).Assembly.Location, "CBObject");

		new Thread (ThreadStart).Start (mbro);
		new Thread (ThreadStart).Start (cbo);
		Thread.Sleep (100);

		AppDomain.Unload (domain);
		return 0;
	}

	static void Worker (object x) {
		Thread.Sleep (100000);
	}

	public static void invoke_workers () {
		for (int i = 0; i < 1; i ++)
			ThreadPool.QueueUserWorkItem (Worker);
	}

	public static int test_0_unload_with_threadpool () {
		AppDomain domain = AppDomain.CreateDomain ("test_0_unload_with_threadpool");

		domain.DoCallBack (new CrossAppDomainDelegate (invoke_workers));
		AppDomain.Unload (domain);

		return 0;
	}

	/*
	 * This test is not very deterministic since the thread which enqueues
	 * the work item might or might not be inside the domain when the unload
	 * happens. So disable this for now.
	 */
	/*
	public static void DoUnload (object state) {
		AppDomain.Unload (AppDomain.CurrentDomain);
	}

	public static void Callback () {
		Console.WriteLine (AppDomain.CurrentDomain);
		WaitCallback unloadDomainCallback = new WaitCallback (DoUnload);
		ThreadPool.QueueUserWorkItem (unloadDomainCallback);
	}		

	public static int test_0_unload_inside_appdomain_async () {
		AppDomain domain = AppDomain.CreateDomain ("Test3");

		domain.DoCallBack (new CrossAppDomainDelegate (Callback));

		return 0;
	}
	*/

	public static void SyncCallback () {
		AppDomain.Unload (AppDomain.CurrentDomain);
	}		

	public static int test_0_unload_inside_appdomain_sync () {
		AppDomain domain = AppDomain.CreateDomain ("Test3");
		bool caught = false;

		try {
			domain.DoCallBack (new CrossAppDomainDelegate (SyncCallback));
		}
		catch (AppDomainUnloadedException ex) {
			caught = true;
		}

		if (!caught)
			return 1;

		return 0;
	}

	public static int test_0_invoke_after_unload () {
		AppDomain domain = AppDomain.CreateDomain ("DeadInvokeTest");
		Bar bar = (Bar)domain.CreateInstanceAndUnwrap (typeof (Tests).Assembly.FullName, "Bar");
		int x;

		if (!RemotingServices.IsTransparentProxy(bar))
			return 3;

		AppDomain.Unload (domain);

		try {
			x = bar.test (123);
			if (x == 124)
				return 1;
			return 2;
		} catch (Exception e) {
			return 0;
		}
	}

	public static int test_0_abort_wait () {
		AppDomain domain = AppDomain.CreateDomain ("AbortWait");
		Bar bar = (Bar)domain.CreateInstanceAndUnwrap (typeof (Tests).Assembly.FullName, "Bar");
		int x;

		bar.start_wait ();
		AppDomain.Unload (domain);
		return 0;
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

