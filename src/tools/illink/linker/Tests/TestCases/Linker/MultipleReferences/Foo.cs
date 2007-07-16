using System;

public class Foo {

	public Bar b;

	public Foo (Bar b)
	{
		this.b = b;
	}

	public void UseBar ()
	{
		b.Bang ();
	}

	[NotLinked] public void Blam ()
	{
	}
}
