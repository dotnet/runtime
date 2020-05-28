using System;
using System.Reflection;

internal class Program
{
	public static int Main (string[] args)
	{
		// newobj Derived
		Derived d = new Derived ();
		// ldvirtftn Base::Foo
		// newobj Del1::.ctor
		Del1 b = new Del1 (d.Foo);
		// ldftn Del1::Invoke
		// newobj Del2::.ctor
		Del2 f = new Del2 (b.Invoke);
		// should call Derived.Foo not Base.Foo
		var r = f ("abcd");
		return r;
	}
}


public delegate int Del1 (string s);
public delegate int Del2 (string s);

public class Base
{
	public virtual int Foo (string s)
	{
		Console.WriteLine ("Base.Foo called. Bad");
		return 1;
	}
}

public class Derived : Base
{
	public override int Foo (string s)
	{
		Console.WriteLine ("Derived.Foo called. Good");
		return 0;
	}
}
