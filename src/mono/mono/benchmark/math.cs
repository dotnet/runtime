using System;

public class MulDiv {

	public static double mathtest (int n) {
		double res = 0;

		for (int j = 0; j < 200000; j++) {
			res += Math.Sin (j);
			res += Math.Cos (j);
		}
		
		return res;
	}
	
	public static int Main (string[] args) {
		int repeat = 1;
		
		if (args.Length == 1)
			repeat = Convert.ToInt32 (args [0]);
		
		Console.WriteLine ("Repeat = " + repeat);

		for (int i = 0; i < (repeat * 50); i++)
			mathtest (1000);
		
		return 0;
	}
}


