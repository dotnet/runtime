
using System;

public class Foo {

	public void Do ()
	{
		Bar b = new Baz ();
		b.Bang ();
	}
}

public class Bar {

	public virtual void Bang ()
	{
	}
}

public class Baz : Bar {

	private string _hey;

	public string Hey {
		[NotLinked] get { return _hey; }
		[NotLinked] set { _hey = value; }
	}

	public override void Bang ()
	{
		Console.WriteLine (_hey);
	}
}

[NotLinked] public class NotLinkedAttribute : Attribute {}