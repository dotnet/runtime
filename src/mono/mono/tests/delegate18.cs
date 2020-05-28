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
		var mi = typeof (Del1).GetMethod ("Invoke");
		if (mi is null)
			return 2;
		// should call Derived.Foo not Base.Foo
		var r = (int) mi.Invoke (b, new object[] {"abcd"});
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
