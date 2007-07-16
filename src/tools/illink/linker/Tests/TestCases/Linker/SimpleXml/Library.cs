using System;

public class Foo {

	int _baz;
	[NotLinked] int _shebang;

	public Foo ()
	{
		_baz = 42;
	}

	public int Baz ()
	{
		return _baz;
	}

	[NotLinked] public int Shebang (int bang)
	{
		return _shebang = bang * 2;
	}
}

public class Bar {

	int _truc;

	public Bar ()
	{
		_truc = 12;
	}

	public int Truc ()
	{
		return _truc;
	}
}

[NotLinked] public class Gazonk {
}

[NotLinked, AttributeUsage (AttributeTargets.All)]
public class NotLinkedAttribute : Attribute {
}
