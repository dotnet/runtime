using System;

public class Gen<A,B,C> {}

public class main {
    static object GenericFunc<A,B,C> () {
	return new Gen<A,B,C> ();
    }

    static void DoGenericStuff () {
	Console.WriteLine ("doing generic stuff");
	GenericFunc<object,object,object> ();
    }

    static void DoOtherGenericStuff () {
	Console.WriteLine ("doing other generic stuff");
	GenericFunc<object,object,int> ();
    }

    public static void Main ()
    {
	// Create an Application Domain:
	System.AppDomain newDomain = System.AppDomain.CreateDomain("NewApplicationDomain");

	// Load and execute an assembly:
	newDomain.ExecuteAssembly(@"generic-unloading-sub.2.exe");

	DoGenericStuff ();

	// Unload the application domain:
	System.AppDomain.Unload(newDomain);

	DoOtherGenericStuff ();
    }
}
