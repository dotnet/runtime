using System;
using System.Threading;
using System.Runtime.Remoting;

public class Test : MarshalByRefObject {
    delegate int GetIntDelegate ();

    static void async_callback (IAsyncResult ar)
    {
	    Console.WriteLine ("Async Callback in domain " + AppDomain.CurrentDomain + " " + ar.AsyncState);
    }

    ~Test () {
	    Console.WriteLine ("in test destructor");
	    GetIntDelegate del = new GetIntDelegate (getInt);
	    AsyncCallback ac = new AsyncCallback (async_callback);
	    if (del.BeginInvoke (ac, "bla") == null) {
		    Console.WriteLine ("async result is null");
		    Environment.Exit (1);
	    }
    }

    public int getInt () {
	    Console.WriteLine ("getInt in " + AppDomain.CurrentDomain);
	    return 123;
    }
}

public class main {
    public static int Main (string [] args) {
	AppDomain domain = AppDomain.CreateDomain ("newdomain");
	int i;

	for (i = 0; i < 200; ++i) {
		domain.CreateInstanceAndUnwrap (typeof (Test).Assembly.FullName, typeof (Test).FullName);
	}

	Console.WriteLine ("unloading");
	AppDomain.Unload (domain);
	Console.WriteLine ("unloaded");

	GC.Collect ();
	GC.WaitForPendingFinalizers ();

	Console.WriteLine ("done");

	return 0;
    }
}
