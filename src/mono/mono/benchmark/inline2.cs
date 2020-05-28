using System;

public class Test {

	public static void test0 () {
		test1 (0);
	}
	public static void test1 (int a) {
		test2 (0, 1);
	}
	public static void test2 (int a, int b) {
		test3 (0, 1, 2);
	}
	public static void test3 (int a, int b, int c) {
		test4 (0, 1, 2, 3);
	}
	public static void test4 (int a, int b, int c, int d) {
	}

	public static int Main (string[] args) {
		int repeat = 1;
		
		if (args.Length == 1)
			repeat = Convert.ToInt32 (args [0]);
		
		Console.WriteLine ("Repeat = " + repeat);

		for (int i = 0; i < repeat; i++)
			for (int j = 0; j < 500000000; j++)
				test0 ();
		
		return 0;
	}
}


