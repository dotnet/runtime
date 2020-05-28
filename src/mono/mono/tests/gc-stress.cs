using System;

class T {
	
	static int count = 1000000;
	static int loops = 20;
	static object obj;
	static object obj2;

	static void work () {
		for (int i = 0; i < count; ++i) {
			obj = new object ();
			obj2 = i;
		}
	}
	static void Main (string[] args) {
		if (args.Length > 0)
			loops = int.Parse (args [0]);
		if (args.Length > 1)
			count = int.Parse (args [1]);
		for (int i = 0; i < loops; ++i) {
			work ();
		}
	}
}

