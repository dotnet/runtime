using System;

public class Test {

	public static int Main (string[] args) {
		int repeat = 1;

		if (args.Length == 1)
			repeat = Convert.ToInt32 (args [0]);
		
		Console.WriteLine ("Repeat = " + repeat);

		object a = new Test ();
		
		for (int i = 0; i < (repeat * 5000); i++)
			for (int j = 0; j < 100000; j++)
				if (!(a is Test))
					return 1;
		
		return 0;
	}
}


