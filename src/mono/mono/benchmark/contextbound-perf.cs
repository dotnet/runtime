using System;
using System.Collections;
using System.Runtime.Remoting;

public class Bar : System.ContextBoundObject {
}

public class Driver {
  public static void Main (string[] args) {
	Bar b = new Bar();
    
	for (int i = 0; i != 30000000; i++)
		if (!b.Equals (b))
			Console.WriteLine ("error!!");
  }
}

