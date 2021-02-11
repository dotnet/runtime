using System;
using System.Threading;
using System.Runtime.Remoting;

public struct Nester<T> {
    public T a, b, c, d;
}

public class main {
    static Nester<Nester<Nester<Nester<Nester<Nester<Nester<Nester<object>>>>>>>> nester;

    public static int Main (string [] args) {
	return 0;
    }
}
