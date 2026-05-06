using System;
using System.IO;

public class Test {

	public static int boxtest ()
	{
		int i = 123;
		object o = i;

		int j = (int) o;

		if (i != j)
			return 1;
		
		return 0;
	}

	public static int Main () {
		string t = 123.ToString();

		if (t != "123")
			return 1;

		if (boxtest () != 0)
			return 1;

		
		return 0;
	}
}


