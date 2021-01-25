using System;
using System.Reflection;

public enum WasCalled {
	BaseWasCalled,
	DerivedWasCalled
}

public delegate WasCalled Del1 (string s);
public delegate WasCalled Del2 (string s);

public class Base
{
	public virtual WasCalled Foo (string s)
	{
		Console.WriteLine ("Base.Foo called. Expected {0}", s);
		return WasCalled.BaseWasCalled;
	}
}

public class Derived : Base
{
	public override WasCalled Foo (string s)
	{
		Console.WriteLine ("Derived.Foo called. Expected {0}", s);
		return WasCalled.DerivedWasCalled;
	}
}


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
		var r = f ("Derived.Foo");
		if (r != WasCalled.DerivedWasCalled)
			return 1;
		// should call Base.Foo not Derived.Foo
		var boundDelegate = (Del2)Activator.CreateInstance (typeof (Del2), b, typeof (Base).GetMethod (nameof (Base.Foo)).MethodHandle.GetFunctionPointer());
		r = boundDelegate ("Base.Foo");
		if (r != WasCalled.BaseWasCalled)
			return 2;
		return 0;
	}
}

