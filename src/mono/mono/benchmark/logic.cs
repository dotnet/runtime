// this code is part of the pnetmark benchmark 

using System;

public class Logic {

	public static void logic_run ()
	{
		int iter;

		// Initialize.
		bool flag1 = true;
		bool flag2 = true;
		bool flag3 = true;
		bool flag4 = true;
		bool flag5 = true;
		bool flag6 = true;
		bool flag7 = true;
		bool flag8 = true;
		bool flag9 = true;
		bool flag10 = true;
		bool flag11 = true;
		bool flag12 = true;
		bool flag13 = true;

		// First set of tests.
		for(iter = 0; iter < 2000000; ++iter) {
			if((flag1 || flag2) && (flag3 || flag4) &&
			   (flag5 || flag6 || flag7))
				{
				flag8 = !flag8;
				flag9 = !flag9;
				flag10 = !flag10;
				flag11 = !flag11;
				flag12 = !flag12;
				flag13 = !flag13;
				flag1 = !flag1;
				flag2 = !flag2;
				flag3 = !flag3;
				flag4 = !flag4;
				flag5 = !flag5;
				flag6 = !flag6;
				flag1 = !flag1;
				flag2 = !flag2;
				flag3 = !flag3;
				flag4 = !flag4;
				flag5 = !flag5;
				flag6 = !flag6;
			}
		}
	}
	
	public static int Main (string[] args) {
		int repeat = 1;
		
		if (args.Length == 1)
			repeat = Convert.ToInt32 (args [0]);
		
		Console.WriteLine ("Repeat = " + repeat);

		for (int i = 0; i < (repeat * 50); i++)
			logic_run ();
		
		return 0;
	}
}


