using System;

public class NestedLoop {
	static public int nest_test () {
		int n = 16;
		int x = 0;
		int a = n;
		while (a-- != 0) {
		    int b = n;
		    while (b-- != 0) {
			int c = n;
			while (c-- != 0) {
			    int d = n;
	    		while (d-- != 0) {
				int e = n;
				while (e-- != 0) {
				    int f = n;
				    while (f-- != 0) {
					x++;
				    }
				}
	    		}
			}
		    }
		}
		return x;
	}

	public static int Main (string[] args) {
		int repeat = 1;
		
		if (args.Length == 1)
			repeat = Convert.ToInt32 (args [0]);
		
		Console.WriteLine ("Repeat = " + repeat);

		for (int i = 0; i < repeat*10; i++)
			if (nest_test () != 16777216)
				return 1;
		
		return 0;
	}
}


