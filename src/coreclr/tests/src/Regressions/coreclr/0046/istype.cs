using System;

public struct MyStruct {}

public class IsType
{
	public static int Main()
	{
		MyStruct? m = default(MyStruct);
		object o = m;

		if (null == m)
		{
			Console.WriteLine("FAIL: o is null");
			return 0;
		}

		if (o is MyStruct)
		{
			Console.WriteLine("PASS");
			return 100;
		}
		else
		{
			Console.WriteLine("FAIL: o is not MyStruct");
			return 0;
		}
	}
}
