using System;
using System.Runtime.ConstrainedExecution;

class P {

	static public int count = 0;

	/*
	public P () {
		Console.WriteLine ("p");
	}
	*/

	~P () {
		count++;
	}
}

class Q : CriticalFinalizerObject {
	static public int count = 0;
	static public int first_p_count = -1;
	static public int last_p_count = 0;
	~Q () {
		count++;
		if (first_p_count < 0)
			first_p_count = P.count;
		last_p_count = P.count;
	}
}

class T : P {

	static void makeP () {
		P p = new P ();
		Q q = new Q ();
		p = null;
		q = null;
	}

	static void callMakeP () {
		makeP ();
	}

	static int Main () {
		for (int i = 0; i < 100; ++i)
			callMakeP ();
		GC.Collect ();
		GC.WaitForPendingFinalizers ();
		Console.WriteLine (P.count);
		Console.WriteLine (Q.count);
		Console.WriteLine (Q.first_p_count);
		Console.WriteLine (Q.last_p_count);
		if (P.count == 0)
			return 1;
		if (Q.first_p_count < P.count)
			return 1;
		return 0;
	}
}
