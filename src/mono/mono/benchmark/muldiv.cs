using System;

public class MulDiv {

	public static int muldiv (int n) {
		int res = n;

		for (int j = 0; j < 1048; j++) {
			for (int i = 0; i < (n * 4); i++) {
				n = (n / 256);
				n++;
				n = n * 128;
			}
		}
		
		return res;
	}
	
	public static int Main (string[] args) {
		int repeat = 1;
		
		if (args.Length == 1)
			repeat = Convert.ToInt32 (args [0]);
		
		Console.WriteLine ("Repeat = " + repeat);

		for (int i = 0; i < (repeat * 50); i++)
			muldiv (1000);
		
		return 0;
	}
}


