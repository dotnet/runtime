using System;

public class Test {

	static readonly int a = 5;
	static readonly int b = 6;

	public static int Main (string[] args) {
		int repeat = 1;
		
		if (args.Length == 1)
			repeat = Convert.ToInt32 (args [0]);
		
		Console.WriteLine ("Repeat = " + repeat);

		for (int i = 0; i < repeat*3; i++) {
			for (int j = 0; j < 100000000; j++) {
				if ((a != 5) || (b != 6))
					return 1;
			}
		}
		
		return 0;
	}
}


