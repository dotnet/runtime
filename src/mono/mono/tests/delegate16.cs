using System;

public static class Program
{
	public static int Main ()
	{
		if (typeof(Delegate).GetMethod("Invoke") != null)
			return 1;

		return 0;
	}
}