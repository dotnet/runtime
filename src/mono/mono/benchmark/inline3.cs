using System;

public class Test {

	public static int test0 ()
	{
		return test1 (1);
	}
	public static int test1 (int a)
	{
		return test2 (a, 2);
	}
	
	public static int test2 (int a, int b)
	{
		return test3 (a, b, 3);
	}
	
	public static int test3 (int a, int b, int c)
	{
		return test4 (a, b, c, 4);
	}
	
	public static int test4 (int a, int b, int c, int d)
	{
		return a + b + c + d;
	}

	public static int run ()
	{
		return test0 ();
	}

	public static int Main (string[] args) {
		int repeat = 1;
		
		if (args.Length == 1)
			repeat = Convert.ToInt32 (args [0]);
		
		Console.WriteLine ("Repeat = " + repeat);

		for (int i = 0; i < repeat; i++)
			for (int j = 0; j < 500000000; j++)
				if (test0 () != 10)
					return 1;
		
		
		return 0;
	}
}


