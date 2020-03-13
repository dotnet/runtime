using System;

public class Gen<A,B,C> {}

public class main {
    static object GenericFunc<A,B,C> () {
	return new Gen<A,B,C> ();
    }

    static void DoGenericStuff () {
	GenericFunc<object,object,object> ();
    }

    public static int Main () {
	DoGenericStuff ();
	return 0;
    }
}
