using System;
                   
class Test {
	public static int Main () {
		int [] n = new int [1];
		int b = 16;
                   
		n [0] = 100 + (1 << (16 - b));
		Console.WriteLine (n [0]);

		if (n [0] != 101)
			return 1;

		return 0;
	}
}

