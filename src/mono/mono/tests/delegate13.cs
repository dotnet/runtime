using System;

public static class Program
{
	public static int Main ()
	{
		Action d1 = new Action (Method1);
		Action d2 = new Action (Method2);
		Action d12 = d1 + d2;
		Action d21 = d2 + d1;

		if (d1.Method.Name != "Method1")
			return 1;
		if (d2.Method.Name != "Method2")
			return 2;
		if (d12.Method.Name != "Method2")
			return 3;
		if (d21.Method.Name != "Method1")
			return 4;

		return 0;
	}

	public static void Method1 ()
	{
	}

	public static void Method2 ()
	{
	}
}