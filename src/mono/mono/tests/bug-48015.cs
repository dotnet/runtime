using System;
using System.Collections;
using System.Runtime.Remoting;

public class Foo : System.ContextBoundObject {
}

public class Bar : System.ContextBoundObject {

	public void Test(Foo f) {
		if (RemotingServices.IsTransparentProxy (f))
			Console.WriteLine ("Bar::Test(Foo) Is TP");
		else
			Console.WriteLine ("Bar::Test(Foo) Is NOT a TP (error!)");

		if (!f.Equals (f))
			Console.WriteLine ("Bar::Test(Foo) f.Equals (b) failed (error!)");
		else
			Console.WriteLine ("Bar::Test(Foo) f.Equals (f) ok!");
	}
}

public class Driver {
  public static void Main (string[] args) {
    Foo f = new Foo();
	Bar b = new Bar();
    
    if (!b.Equals (b))
    	Console.WriteLine ("b.Equals (b) failed (error!)");
    else
        Console.WriteLine ("b.Equals (b) ok!");

    if (RemotingServices.IsTransparentProxy (b))
		Console.WriteLine ("b is a TP");
	else
		Console.WriteLine ("b is NOT a TP (error!)");

	b.Test(f);

	if (!f.Equals (f))
		Console.WriteLine ("f.Equals (b) failed (error!)");
	else
		Console.WriteLine ("f.Equals (f) ok!");

	Console.WriteLine ("test end.");
  }
}

