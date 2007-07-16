using System;

public class Program {

	public static int Main ()
	{
		Foo f = new Foo (new Bar ());
		f.UseBar ();

		Baz b = new Baz ();
		b.Chain (f);

		return 0;
	}
}
