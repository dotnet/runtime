using System;
using System.Threading;

/*
This test checks if the AddDomain::DomainUnload event is processed with
a fully working domain. In special if the threadpool remains operational.
*/
class Driver
{
	static void UnloadHook (object obj, EventArgs args)
	{
		ManualResetEvent evt = new ManualResetEvent (false);
		Console.WriteLine ("On the UnloadHook");
		if (Environment.HasShutdownStarted)
			throw new Exception ("Environment.HasShutdownStarted must not be true");
		Action<int> f = (int x) => {
			evt.WaitOne (1000);
			evt.Set ();
		};
		f.BeginInvoke (1, null, null);
		evt.WaitOne ();
		Console.WriteLine ("Hook done");
	}

	static void OtherDomain()
	{
		AppDomain app = AppDomain.CurrentDomain;
		Console.WriteLine ("Now I'm on {0}", app);
		app.DomainUnload += Driver.UnloadHook;
	}

	static int Main ()
	{
		AppDomain app = AppDomain.CreateDomain ("Foo");
		Console.WriteLine ("I'm on {0}", AppDomain.CurrentDomain);
		app.DoCallBack (Driver.OtherDomain );

		Thread.Sleep (1);
		AppDomain.Unload (app);
		Thread.Sleep (1);
		return 0;
	}
}

