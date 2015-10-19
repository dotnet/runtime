using System;

public class Nullable
{

	public static bool BoxUnboxToNQ(object o)
	{
		return ((int)(ValueType)o == (int)55);
	}

	public static bool Run()
	{
		int? i = 55;
		
		return BoxUnboxToNQ(i);
	}

	public static int Main()
	{
		if (Run())
		{
			Console.WriteLine("PASS");
			return 100;
		}
		else
		{
			Console.WriteLine("FAIL");
			return 0;
		}
	}
}
