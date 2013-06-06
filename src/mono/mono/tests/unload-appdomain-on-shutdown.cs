using System;
using System.Reflection;
using System.Threading;


class Driver {
	public static void Bla ()
	{
		//DoDomainUnload is invoked as part of the unload sequence, so let's pre jit it here to increase the likehood
		//of hanging
		var m = typeof (AppDomain).GetMethod ("DoDomainUnload", BindingFlags.Instance | BindingFlags.NonPublic);
		if (m != null)
			m.MethodHandle.GetFunctionPointer (); 
	}

	static AppDomain ad;
	static ManualResetEvent evt = new ManualResetEvent (false);
	
	static void UnloadIt ()
	{
		//AppDomain.Unload calls AppDomain::getDomainId () before calling into the runtime, so let's pre jit 
		//it here to increase the likehood of hanging
		var x = ad.Id;
		evt.Set ();
		AppDomain.Unload (ad);
	}
	static int Main ()
	{
		AppDomain.Unload (AppDomain.CreateDomain ("Warmup unload code"));
		Console.WriteLine (".");
		ad = AppDomain.CreateDomain ("NewDomain");
		ad.DoCallBack (Bla);
		var t = new Thread (UnloadIt);
		t.IsBackground = true;
		t.Start ();
		evt.WaitOne ();
		return 0;
	}
}
