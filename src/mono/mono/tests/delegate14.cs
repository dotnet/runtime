using System;

// Regression test for bug #58888

public static class Program
{
	public static int Main (string[] args)
	{
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
}
