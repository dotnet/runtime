using System;
using System.Threading;
using System.Runtime.Remoting;
using System.Reflection;

public class JustSomeClass {
}

public class Test2 : ContextBoundObject
{
	public void Run () {
		Thread.CurrentThread.Abort ();
	}
}

public class Test1 : MarshalByRefObject
{
	public bool Run () {
		AppDomain d = AppDomain.CreateDomain ("foo2");

		var t2 = (Test2)d.CreateInstanceAndUnwrap (Assembly.GetExecutingAssembly().FullName, "Test2");
		try {
			t2.Run ();
		} catch (ThreadAbortException ex) {
			Thread.ResetAbort ();
			return true;
		}

		return false;
	}
}

public class Test : MarshalByRefObject {
	ThreadAbortException exc;
	public JustSomeClass other;

	public void doThrow (int n, object state) {
		if (n <= 0)
			Thread.CurrentThread.Abort (state);
		else
			doThrow (n - 1, state);
	}

	public void abortProxy () {
		doThrow (10, this);
	}

	public void abortOther () {
		other = new JustSomeClass ();
		doThrow (10, other);
	}

	public void abortString () {
		try {
			doThrow (10, "bla");
		} catch (ThreadAbortException e) {
			exc = e;
		}
	}

	public void abortOtherIndirect (Test test) {
		test.abortOther ();
	}

	public object getState () {
		return exc.ExceptionState;
	}

	public int getInt () {
		return 123;
	}
}

public static class Tests
{
	static AppDomain domain = AppDomain.CreateDomain ("newdomain");

	public static int test_0_abort_other_indirect ()
	{
		Test test = (Test) domain.CreateInstanceAndUnwrap (typeof (Test).Assembly.FullName, typeof (Test).FullName);
		Test testHere = new Test ();

		if (!RemotingServices.IsTransparentProxy (test)) {
			Console.WriteLine ("test is no proxy");
			return 1;
		}

		try {
			test.abortOtherIndirect (testHere);
		} catch (ThreadAbortException e) {
			object state = e.ExceptionState;
			Thread.ResetAbort ();
			if ((JustSomeClass)state != testHere.other) {
				Console.WriteLine ("other class not preserved in state");
				return 2;
			}
		}

		return 0;
	}

	public static int test_0_abort_string ()
	{
		Test test = (Test) domain.CreateInstanceAndUnwrap (typeof (Test).Assembly.FullName, typeof (Test).FullName);

		if (!RemotingServices.IsTransparentProxy (test)) {
			Console.WriteLine ("test is no proxy");
			return 1;
		}

		try {
			test.abortString ();
			Console.WriteLine ("no abort");
			return 2;
		} catch (ThreadAbortException e) {
			object state;
			state = e.ExceptionState;
			Thread.ResetAbort ();
			if (state == null) {
				Console.WriteLine ("state is null");
				return 3;
			} else {
				if (RemotingServices.IsTransparentProxy (state)) {
					Console.WriteLine ("state is proxy");
					return 4;
				}
				if (!((string)state).Equals ("bla")) {
					Console.WriteLine ("state is wrong: " + (string)state);
					return 5;
				}
			}
			if (RemotingServices.IsTransparentProxy (e)) {
				Console.WriteLine ("exception is proxy");
				return 6;
			}
			if (test.getState () != null) {
				Console.WriteLine ("have state");
				return 7;
			}
		}

		return 0;
	}

	public static int test_0_abort_proxy ()
	{
		Test test = (Test) domain.CreateInstanceAndUnwrap (typeof (Test).Assembly.FullName, typeof (Test).FullName);

		if (!RemotingServices.IsTransparentProxy (test)) {
			Console.WriteLine ("test is no proxy");
			return 1;
		}

		try {
			test.abortProxy ();
			Console.WriteLine ("no abort");
			return 2;
		} catch (ThreadAbortException e) {
			object state;
			state = e.ExceptionState;
			Thread.ResetAbort ();
			if (state == null) {
				Console.WriteLine ("state is null");
				return 3;
			} else {
				if (!RemotingServices.IsTransparentProxy (state)) {
					Console.WriteLine ("state is not proxy");
					return 4;
				}
				if (((Test)state).getInt () != 123) {
					Console.WriteLine ("state doesn't work");
					return 5;
				}
			}
			if (RemotingServices.IsTransparentProxy (e)) {
				Console.WriteLine ("exception is proxy");
				return 6;
			}
		}

		return 0;
	}

	public static int test_0_abort_other ()
	{
		Test test = (Test) domain.CreateInstanceAndUnwrap (typeof (Test).Assembly.FullName, typeof (Test).FullName);

		if (!RemotingServices.IsTransparentProxy (test)) {
			Console.WriteLine ("test is no proxy");
			return 1;
		}

		try {
			test.abortOther ();
			Console.WriteLine ("no abort");
			return 2;
		} catch (ThreadAbortException e) {
			object state = null;
			bool stateExc = false;

			try {
				state = e.ExceptionState;
				Console.WriteLine ("have state");
			} catch (Exception) {
				stateExc = true;
				/* FIXME: if we put this after the try/catch, mono
				   quietly quits */
				Thread.ResetAbort ();
			}
			if (!stateExc) {
				Console.WriteLine ("no state exception");
				return 3;
			}

			if (RemotingServices.IsTransparentProxy (e)) {
				Console.WriteLine ("exception is proxy");
				return 4;
			}
		}

		return 0;
	}

	public static int test_0_abort_in_thread ()
	{
		// #539394
		// Calling Thread.Abort () from a remoting call throws a ThreadAbortException which
		// cannot be caught because the exception handling code is confused by the domain
		// transitions
		bool res = false;

		Thread thread = new Thread (delegate () {
			AppDomain d = AppDomain.CreateDomain ("foo");

			var t = (Test1)d.CreateInstanceAndUnwrap (Assembly.GetExecutingAssembly().FullName, "Test1");
			res = t.Run ();
		});

		thread.Start ();
		thread.Join ();

		if (!res)
			return 1;

		return 0;
	}

	public static int Main (string [] args)
	{
		return TestDriver.RunTests (typeof (Tests), args);
	}
}
