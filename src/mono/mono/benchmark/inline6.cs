using System;

public class Test {

	public static int test (int n) {
		int x = n + 1;

		return x;
	}

	public static int Main (string[] args) {
		int repeat = 1;

		/*
		if (args.Length == 1)
			repeat = Convert.ToInt32 (args [0]);
		
		Console.WriteLine ("Repeat = " + repeat);
		*/
		
		for (int i = 0; i < repeat; i++)
			for (int j = 0; j < 500000000; j++)
				test (12345);
		
		return 0;
	}
}


