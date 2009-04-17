using System;
using System.Threading;
using System.Runtime.Remoting;

public class JustSomeClass {
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

public class main {
    public static int Main (string [] args) {
	AppDomain domain = AppDomain.CreateDomain ("newdomain");
	Test test = (Test) domain.CreateInstanceAndUnwrap (typeof (Test).Assembly.FullName, typeof (Test).FullName);
	bool didAbort;
	Test testHere = new Test ();

	if (!RemotingServices.IsTransparentProxy (test)) {
	    Console.WriteLine ("test is no proxy");
	    return 5;
	}

	try {
	    test.abortOtherIndirect (testHere);
	} catch (ThreadAbortException e) {
	    object state = e.ExceptionState;
	    Thread.ResetAbort ();
	    if ((JustSomeClass)state != testHere.other) {
		Console.WriteLine ("other class not preserved in state");
		return 16;
	    }
	}

	try {
	    didAbort = false;
	    test.abortString ();
	} catch (ThreadAbortException e) {
	    object state;
	    state = e.ExceptionState;
	    Thread.ResetAbort ();
	    didAbort = true;
	    if (state == null) {
		    Console.WriteLine ("state is null");
		    return 13;
	    } else {
		    if (RemotingServices.IsTransparentProxy (state)) {
			    Console.WriteLine ("state is proxy");
			    return 1;
		    }
		    if (!((string)state).Equals ("bla")) {
			    Console.WriteLine ("state is wrong: " + (string)state);
			    return 2;
		    }
	    }
	    if (RemotingServices.IsTransparentProxy (e)) {
		Console.WriteLine ("exception is proxy");
		return 3;
	    }
	    if (test.getState () != null) {
		Console.WriteLine ("have state");
		return 12;
	    }
	}
	if (!didAbort) {
	    Console.WriteLine ("no abort");
	    return 4;
	}

	try {
	    didAbort = false;
	    test.abortProxy ();
	} catch (ThreadAbortException e) {
	    object state;
	    state = e.ExceptionState;
	    Thread.ResetAbort ();
	    didAbort = true;
	    if (state == null) {
		    Console.WriteLine ("state is null");
		    return 14;
	    } else {
		    if (!RemotingServices.IsTransparentProxy (state)) {
			    Console.WriteLine ("state is not proxy");
			    return 6;
		    }
		    if (((Test)state).getInt () != 123) {
			    Console.WriteLine ("state doesn't work");
			    return 15;
		    }
	    }
	    if (RemotingServices.IsTransparentProxy (e)) {
		    Console.WriteLine ("exception is proxy");
		    return 7;
	    }
	}
	if (!didAbort) {
	    Console.WriteLine ("no abort");
	    return 8;
	}

	try {
	    didAbort = false;
	    test.abortOther ();
	} catch (ThreadAbortException e) {
	    object state = null;
	    bool stateExc = false;

	    didAbort = true;

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
		return 9;
	    }

	    if (RemotingServices.IsTransparentProxy (e)) {
		Console.WriteLine ("exception is proxy");
		return 10;
	    }
	}
	if (!didAbort) {
	    Console.WriteLine ("no abort");
	    return 11;
	}

	Console.WriteLine ("done");

	return 0;
    }
}
