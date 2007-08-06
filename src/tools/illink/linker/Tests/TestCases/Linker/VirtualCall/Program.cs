using System;

public class Program {

	public static int Main ()
	{
		Program p = new Program ();
		return p.Run ();
	}

	int Run ()
	{
		Library lib = new PowerFulLibrary ();
		return lib.Shebang ();
	}
}
