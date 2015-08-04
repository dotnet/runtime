using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class CustomException : Exception
{
}

class CustomException2 : Exception
{
}


class CrossDomain : MarshalByRefObject
{
	public Action NewDelegateWithTarget ()
	{
		return new Action (Bar);
	}

	public Action NewDelegateWithoutTarget ()
	{
		return () => { throw new CustomException (); };
	}

	public void Bar ()
	{
		throw new CustomException ();
	}
}

class Driver {
	static ManualResetEvent mre = new ManualResetEvent (false);

	static void DoTest1 ()
	{
		mre.Reset ();

		var t = new Thread (new ThreadStart (() => { try { throw new CustomException (); } finally { mre.Set (); } }));
		t.Start ();

		if (!mre.WaitOne (5000))
			Environment.Exit (2);

		t.Join ();
	}

	static void DoTest2 ()
	{
		mre.Reset ();

		var a = new Action (() => { try { throw new CustomException (); } finally { mre.Set (); } });
		var ares = a.BeginInvoke (null, null);

		if (!mre.WaitOne (5000))
			Environment.Exit (2);

		try {
			a.EndInvoke (ares);
			throw new Exception ();
		} catch (CustomException) {			
		} catch (Exception) {
			Environment.Exit (3);
		}
	}

	static void DoTest3 ()
	{
		mre.Reset ();

		ThreadPool.QueueUserWorkItem (_ => { try { throw new CustomException (); } finally { mre.Set (); } });

		if (!mre.WaitOne (5000))
			Environment.Exit (2);
	}

	static void DoTest4 ()
	{
		mre.Reset ();

		var t = Task.Factory.StartNew (new Action (() => { try { throw new CustomException (); } finally { mre.Set (); } }));

		if (!mre.WaitOne (5000))
			Environment.Exit (2);

		try {
			t.Wait ();
			throw new Exception ();
		} catch (AggregateException ae) {
			if (!(ae.InnerExceptions [0] is CustomException))
				Environment.Exit (4);
		} catch (Exception) {
			Environment.Exit (3);
		}
	}
	
	class FinalizedClass
	{
		~FinalizedClass ()
		{
			try {
				throw new CustomException ();
			} finally {
				mre.Set ();
			}
		}
	}

	static void DoTest5 ()
	{
		mre.Reset ();

		new FinalizedClass();

		GC.Collect ();
		GC.WaitForPendingFinalizers ();

		if (!mre.WaitOne (5000))
			Environment.Exit (2);
	}

	static void DoTest6 ()
	{
		ManualResetEvent mre2 = new ManualResetEvent (false);

		mre.Reset ();

		var a = new Action (() => { try { throw new CustomException (); } finally { mre.Set (); } });
		var ares = a.BeginInvoke (_ => { mre2.Set (); throw new CustomException2 (); }, null);

		if (!mre.WaitOne (5000))
			Environment.Exit (2);
		if (!mre2.WaitOne (5000))
			Environment.Exit (22);

		try {
			a.EndInvoke (ares);
			throw new Exception ();
		} catch (CustomException) {
		} catch (Exception) {
			Environment.Exit (3);
		}
	}

	static void DoTest7 ()
	{
		var cd = (CrossDomain) AppDomain.CreateDomain ("ad").CreateInstanceAndUnwrap (typeof(CrossDomain).Assembly.FullName, "CrossDomain");

		var a = cd.NewDelegateWithoutTarget ();
		var ares = a.BeginInvoke (delegate { throw new CustomException2 (); }, null);

		try {
			a.EndInvoke (ares);
			throw new Exception ();
		} catch (CustomException) {
		} catch (Exception) {
			Environment.Exit (3);
		}
	}

	static void DoTest8 ()
	{
		var cd = (CrossDomain) AppDomain.CreateDomain ("ad").CreateInstanceAndUnwrap (typeof(CrossDomain).Assembly.FullName, "CrossDomain");

		var a = cd.NewDelegateWithTarget ();
		var ares = a.BeginInvoke (delegate { throw new CustomException2 (); }, null);

		try {
			a.EndInvoke (ares);
			throw new Exception ();
		} catch (CustomException) {
		} catch (Exception) {
			Environment.Exit (3);
		}
	}

	static void Main (string[] args)
	{
		switch (int.Parse (args [0])) {
		case 1: DoTest1 (); break;
		case 2: DoTest2 (); break;
		case 3: DoTest3 (); break;
		case 4: DoTest4 (); break;
		case 5: DoTest5 (); break;
		case 6: DoTest6 (); break;
		case 7: DoTest7 (); break;
		case 8: DoTest8 (); break;
		default: throw new ArgumentOutOfRangeException ();
		}
		Environment.Exit (0);
	}
}
