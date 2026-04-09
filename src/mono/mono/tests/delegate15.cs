using System;
using System.Reflection;

public static class Program
{
	class BaseType <T1, T2>
	{
		private T1 a;
		private T2 b;

		public static void blah () { }
	}

	class DerivedType <T> : BaseType <string, T>
	{
		public static void blah2 () { }
	}

	public static int Main (string[] args)
	{
		MethodInfo method = typeof (BaseType<,>).GetMethod ("blah");
		Delegate del = Delegate.CreateDelegate (typeof (Action), null, method, true);
		bool caught = false;

		try {
			((Action)del) ();
		} catch (InvalidOperationException) {
			caught = true;
		}

		if (!caught) {
			Console.WriteLine ("1");
			return 1;
		}

		method = typeof (DerivedType<>).GetMethod ("blah2");
		del = Delegate.CreateDelegate (typeof (Action), null, method, true);
		caught = false;

		try {
			((Action)del) ();
		} catch (InvalidOperationException) {
			caught = true;
		}

		if (!caught) {
			Console.WriteLine ("2");
			return 2;
		}

		Console.WriteLine ("OK");
		return 0;
	}
}
