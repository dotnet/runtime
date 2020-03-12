using System;
using System.Threading;
using System.Runtime.Remoting;

// Does a foreign domain's thread object persist (in .NET) even if it
// hasn't been started?
//
// Insubstantial, because it can't be "moved" to another domain.

// Can we start a foreign domain's thread (i.e. does the thread then
// switch to the foreign domain and execute the start method there)?
//
// No, we can't start it from another domain, because we can't bring
// to another domain.

// What if we start a foreign domain's thread if the domain is gone?
//
// See above.

public class Test : MarshalByRefObject {
    public Thread thread;
    public String str;

    public void setThread () {
	Console.WriteLine ("setting thread");
	thread = Thread.CurrentThread;
	thread.Name = "foo";
    }

    public void setStr (string s) {
	Console.WriteLine ("setting str");
	str = s;
    }

    public void callSetThread (Test t) {
	Thread thread = new Thread (new ThreadStart (t.setThread));

	thread.Start ();
	thread.Join ();

	t.setStr ("a" + "b");
    }
}

public class main {
    public static int Main (string [] args) {
	AppDomain domain = AppDomain.CreateDomain ("newdomain");
	Test myTest = new Test ();
	Test otherTest = (Test) domain.CreateInstanceAndUnwrap (typeof (Test).Assembly.FullName, typeof (Test).FullName);

	otherTest.callSetThread (myTest);

	if (myTest.thread.GetType () == Thread.CurrentThread.GetType ())
		Console.WriteLine ("same type");
	else {
		Console.WriteLine ("different type");
		return 1;
	}

	AppDomain.Unload (domain);

	GC.Collect ();
	GC.WaitForPendingFinalizers ();

	Console.WriteLine ("thread " + myTest.thread);

	Console.WriteLine ("str " + myTest.str);

	if (!myTest.thread.Name.Equals("foo"))
		return 1;

	return 0;
    }
}
