using System;

// Regression test for bug #58888

public static class Program
{
	public static int Main (string[] args)
	{
		// calling delegate on extension method with null target is allowed
		Func<int> func = null;
		if (CallFunc(func.CallFuncIfNotNull) != 0)
			return 2;

		// constructing delegate on instance method with null target should throw
		ITest obj = null;
		try
		{
			GC.KeepAlive((Action)obj.Func);
		}
		catch (NullReferenceException)
		{
			return 0;
		}

		return 1;
	}

	interface ITest
	{
		void Func ();
	}

	static int CallFunc(Func<int> func)
	{
		return func();
	}
}

public static class FuncExtensions
{
	public static int CallFuncIfNotNull(this Func<int> func)
	{
		if (func != null)
			return func();

		return 0;
	}
}