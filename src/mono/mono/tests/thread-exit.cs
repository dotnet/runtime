using System;
using System.Threading;

class FibThread {
    static long fib (long n) {
	if (n < 2)
	    return n;
	else
	    return fib (n - 1) + fib (n - 2);
    }

    public void work () {
	for (long i = 0; i < 30; ++i)
	    Console.WriteLine (fib (i).ToString ());
	Console.WriteLine ("exiting");
	System.Environment.Exit (0);
    }
}

public class Test {
    public static Thread newThread () {
	FibThread ft = new FibThread ();
	return new Thread (new ThreadStart (ft.work));
    }

    static int Main () {
	Thread t = newThread ();
	t.Start ();
	Console.WriteLine ("started");
	return 1;
    }
}
