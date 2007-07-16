using System;

public class Foo {

	void Bar ()
	{
		int i = 42;
		string s = "hey";

		Baz<int> bi = new Baz<int> (i);
		bi.Gazonk ();
		bi.Bat<string>(i, s);

		Baz<string> bs = new Baz<string> (s);
		bs.Gazonk ();
		bs.Bat<int>(s, i);
		bs.BiroBiro ();

		bs.Blam<Bang> ();
	}
}

public class Baz<T> {

	T _t;

	public Baz (T t)
	{
		_t = t;
	}

	public void Gazonk ()
	{
		Console.WriteLine (_t);
	}

	public void Bat<M> (T t, M m)
	{
		Console.WriteLine ("{0}{1}", t, m);
	}

	public void Blam<M> ()
	{
	}

	public T [] BiroBiro ()
	{
		return new T [0];
	}
}

class Bang {

	[NotLinked] public Bang ()
	{
	}
}

[NotLinked, AttributeUsage (AttributeTargets.All)]
public class NotLinkedAttribute : Attribute {
}
