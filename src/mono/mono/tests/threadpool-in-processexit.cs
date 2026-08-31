using System;
using System.Threading;

class Program
{
	static AutoResetEvent mre = new AutoResetEvent(false);

	static void Main ()
	{
		AppDomain.CurrentDomain.ProcessExit += SomeEndOfProcessAction;
	}

	static void SomeEndOfProcessAction(object sender, EventArgs args)
	{
		ThreadPool.QueueUserWorkItem (new WaitCallback (ThreadPoolCallback));
		if (mre.WaitOne(1000))
			Console.WriteLine ("PASS");
		else
			Console.WriteLine ("FAIL");
	}

	static void ThreadPoolCallback (object state)
	{
		mre.Set ();
	}
}
