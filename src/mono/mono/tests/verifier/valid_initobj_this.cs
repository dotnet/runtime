using System;

public struct Foo<T>
{
	public T t;

	public Foo (T t)
	{
		this = new Foo<T> ();
		this.t = t;
	}
}

class MainClass
{

	public static void Main(string[] args)
	{
		var f = new Foo<int> (99);
	}
}


