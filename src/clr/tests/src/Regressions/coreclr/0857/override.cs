using System;

public class Test
{
	public static int Main()
	{
		Test t = new Test();

		if (t.ToString().Equals("Hi"))
		{
			return 100;
		}
		

		return 0;
	}

	public override string ToString()
	{
		return "Hi";
	}
}
