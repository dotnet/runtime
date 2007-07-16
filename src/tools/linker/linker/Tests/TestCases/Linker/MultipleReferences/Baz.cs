using System;

public class Baz {

	public void Chain (Foo f)
	{
		f.b.Bang ();
	}

	[NotLinked] public void Lurman ()
	{
	}
}
