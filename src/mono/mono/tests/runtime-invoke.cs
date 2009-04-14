using System;
using System.Reflection;

public struct A
{
	public override string ToString ()
	{
		return "A";
	}
}

public class D
{
	public string Test ()
	{
		return "Test";
	}
}

enum Enum1
{
	A,
	B
}

enum Enum2
{
	C,
	D
}

class X
{
	public static Enum1 return_enum1 () {
		return Enum1.A;
	}

	public static Enum2 return_enum2 () {
		return Enum2.C;
	}

	static int Main ()
	{
		Assembly ass = Assembly.GetCallingAssembly ();
		Type a_type = ass.GetType ("A");
		MethodInfo a_method = a_type.GetMethod ("ToString");

		Type d_type = ass.GetType ("D");
		MethodInfo d_method = d_type.GetMethod ("Test");

		Console.WriteLine ("TEST: {0} {1}", a_method, d_method);

		A a = new A ();
		D d = new D ();

		object a_ret = a_method.Invoke (a, null);
		Console.WriteLine (a_ret);

		object d_ret = d_method.Invoke (d, null);
		Console.WriteLine (d_ret);

		/* Check sharing of wrappers returning enums */
		if (typeof (X).GetMethod ("return_enum1").Invoke (null, null).GetType () != typeof (Enum1))
			return 1;
		if (typeof (X).GetMethod ("return_enum2").Invoke (null, null).GetType () != typeof (Enum2))
			return 2;

		return 0;
	}
}
