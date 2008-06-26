using System;

public class Program {

	public static int Main ()
	{
		int?[] x = new int?[] { null };
		return x.GetValue (0) == null ? 0 : 1;
	}
}