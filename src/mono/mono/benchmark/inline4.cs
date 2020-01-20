using System;

public class Tests {

	public static int test (int n) {
		if ((n & 1) == 0)
			return 2;
		else
			return 1;
	}

	public static int Main (string[] args) {
		int repeat = 1;
		int sum = 0;
		
		if (args.Length == 1)
			repeat = Convert.ToInt32 (args [0]);
		
		Console.WriteLine ("Repeat = " + repeat);

		for (int i = 0; i < repeat; i++)
			for (int j = 0; j < 50000000; j++)
				sum += test (j);

		
		Console.WriteLine (sum);
		if (sum != (75000000 * repeat))
			return 1;
		
		return 0;
	}
}


