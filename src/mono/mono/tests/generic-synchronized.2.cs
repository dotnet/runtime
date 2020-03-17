using System;
using System.Threading;
using System.Runtime.CompilerServices;

public class Gen<T> {
    [MethodImplAttribute(MethodImplOptions.Synchronized)]
    public int synch () {
	return 123;
    }

    public int callSynch () {
	return synch ();
    }
}

public class main {
    public static int Main () {
	Gen<string> gs = new Gen<string> ();

	gs.synch ();
	gs.callSynch ();

	return 0;
    }
}
