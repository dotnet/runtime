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

class X
{
	static void Main ()
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
	}
}
