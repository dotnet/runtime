using System;

public class Fib {

	public static int fib (int n) {
		if (n < 2)
			return 1;
		return fib(n-2)+fib(n-1);
	}
	public static int Main (string[] args) {
		int repeat = 1;
		
		if (args.Length == 1)
			repeat = Convert.ToInt32 (args [0]);
		
		Console.WriteLine ("Repeat = " + repeat);

		for (int i = 0; i < (repeat * 50); i++)
			if (fib (32) != 3524578)
				return 1;
		
		return 0;
	}
}


