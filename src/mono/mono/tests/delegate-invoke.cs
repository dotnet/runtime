using System;
using System.Reflection;
using System.Globalization;
using System.Diagnostics;

struct EmptyStruct {
	public int value;

	public int test() {
		return value + 10;
	}

	public static int test2 (ref EmptyStruct foo) {
		return foo.value + 20;
	}
}

delegate int ActionRef (ref EmptyStruct _this);
delegate int ActionRef2 ();
delegate int ActionRef3 (EmptyStruct _this);

public struct Foo {
	public int x,y,z;
	public Foo (int i) {
		x = y = z = i;
	}
}

delegate Foo NoArgDele ();
delegate Foo OneArgDele (Driver d);


public class Driver
{
	public static Foo M0 () { return new Foo (10); }
	public Foo M1 () { return new Foo (this == null ? 10 : 20); }
	public static Foo M2 (string x) { return new Foo (30); }
	public static Foo M2i (int x) { return new Foo (40); }
	public static Foo M3 (object x) { return new Foo (50); }
	public virtual Foo M4 () { return new Foo (this == null ? 60 : 70); }

	public static int Main () {
		int r = test_0_refobj_invokes ();
		if (r != 0)
			return r;
		r = test_0_valuetype_invokes ();
		if (r != 0)
			return r + 20;
		return 0;
	}

	public static int test_0_refobj_invokes ()
	{
		NoArgDele x;
		OneArgDele y;

		x = Driver.M0;
		if (x ().x != 10)
			return 1;

		x = new Driver ().M1;
		if (x ().x != 20)
			return 2;

		x = (NoArgDele)Delegate.CreateDelegate (typeof (NoArgDele), "10", typeof (Driver).GetMethod ("M2"));
		if (x ().x != 30)
			return 3;

		x = (NoArgDele)Delegate.CreateDelegate (typeof (NoArgDele), null, typeof (Driver).GetMethod ("M3"));
		if (x ().x != 50)
			return 4;

		y = (OneArgDele)Delegate.CreateDelegate (typeof (OneArgDele), null, typeof (Driver).GetMethod ("M4"));
		if (y (new Driver ()).x != 70)
			return 5;

		x = (NoArgDele)Delegate.CreateDelegate (typeof (NoArgDele), null, typeof (Driver).GetMethod ("M1"));
		if (x ().x != 10)
			return 6;

		x = (NoArgDele)Delegate.CreateDelegate (typeof (NoArgDele), null, typeof (Driver).GetMethod ("M4"));
		if (x ().x != 60)
			return 7;

		return 0;
	}

    public static int test_0_valuetype_invokes ()
    {
		EmptyStruct es = default (EmptyStruct);
		es.value = 100;

		var ar1 = (ActionRef)Delegate.CreateDelegate(typeof (ActionRef), typeof (EmptyStruct).GetMethod("test"));
		if (ar1 (ref es) != 110) {
			Console.WriteLine ("expected 110, got {0}", ar1 (ref es));
			return 1;
		}

		var ar2 = (ActionRef2)Delegate.CreateDelegate (typeof (ActionRef2), null, typeof (EmptyStruct).GetMethod("test"));
		try {
			Console.WriteLine ("must not return, got {0}", ar2 ());
			return 2;
		} catch (NullReferenceException) {
		}

		ar1 = (ActionRef) Delegate.CreateDelegate(typeof (ActionRef), typeof (EmptyStruct).GetMethod("test2"));
		if (ar1 (ref es) != 120) {
			Console.WriteLine ("expected 120, got {0}", ar1 (ref es));
			return 3;
		}

		ar2 = (ActionRef2) Delegate.CreateDelegate(typeof (ActionRef2), es, typeof (EmptyStruct).GetMethod("test"));
		if (ar2 () != 110) {
			Console.WriteLine ("expected 110 got {0}", ar2 ());
			return 4;
		}

		try {
			Delegate.CreateDelegate(typeof (ActionRef2), new EmptyStruct (), typeof (EmptyStruct).GetMethod("test2"));
			Console.WriteLine ("must fail/2");
			return 5;
		} catch (ArgumentException) {}

		try {
			Delegate.CreateDelegate(typeof (ActionRef3), typeof (EmptyStruct).GetMethod("test"));
			Console.WriteLine ("must fail/2");
			return 6;
		} catch (ArgumentException) {}

		return 0;
	}
}