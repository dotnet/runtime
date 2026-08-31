using System;

class MainClass
{
	public static int Main(string[] args)
	{
		DerivedClass o = new DerivedClass();

		Func<string> del1 = GetDel1 (o);
		Func<string> del2 = GetDel2 (o);


		Console.WriteLine("Action\n======\nReflected type: {0}\nDeclaring type: {1}\nAttributes: {2}\nResult: {3}",
			del1.Method.ReflectedType, del1.Method.DeclaringType, del1.Method.Attributes, del1 ());

		Console.WriteLine ();

		Console.WriteLine("Delegate\n========\nReflected type: {0}\nDeclaring type: {1}\nAttributes: {2}\nResult: {3}",
			del2.Method.ReflectedType, del2.Method.DeclaringType, del2.Method.Attributes, del2 ());

		if (del1.Method.ReflectedType != typeof (DerivedClass))
			return 10;
		if (del1.Method.DeclaringType != typeof (DerivedClass))
			return 11;
		if (del1 () != "Derived method")
			return 12;

		if (del2.Method.ReflectedType != typeof (DerivedClass))
			return 20;
		if (del2.Method.DeclaringType != typeof (DerivedClass))
			return 21;
		if (del2 () != "Derived method")
			return 22;

		if (!del1.Equals (del2))
			return 30;
		if (!del2.Equals (del1))
			return 31;

		return 0;
	}

	static Func<string> GetDel1 (DerivedClass o)
	{
		return o.GetMethod();
	}

	static Func<string> GetDel2 (DerivedClass o)
	{
		return (Func<string>) Delegate.CreateDelegate(typeof(Func<string>), o, o.GetMethod().Method);
	}
}

class BaseClass
{
	public Func<string> GetMethod()
	{
		return MyMethod;
	}

	public virtual string MyMethod()
	{
		return "Base method";
	}
}

class DerivedClass : BaseClass
{
	public override string MyMethod()
	{
		return "Derived method";
	}
}
