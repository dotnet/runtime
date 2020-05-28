using System;

public class Tests {

	public static int Main (string[] args) {
		int res, repeat = 1;
		string ts1 = "abcdefghijklmnopqrstuvwxyz";
		
		if (args.Length == 1)
			repeat = Convert.ToInt32 (args [0]);
		
		Console.WriteLine ("Repeat = " + repeat);

		int len = ts1.Length;
			
		for (int i = 0; i < (repeat * 50); i++) {
			for (int j = 0; j < 100000; j++) {
				int k, h = 0;

				for (k = 0; k < len; ++k)
					h += ts1 [k];

			}
		}
		
		return 0;
	}
}


