using System;

class T {
	
	static int count = 1000000;
	static int loops = 20;
	static int persist_factor = 10;

	static object obj;

	static object[] persist;
	static int persist_idx;

	static void work () {
		persist = new object[count / persist_factor + 1];
		persist_idx = 0;
		for (int i = 0; i < count; ++i) {
			obj = new object ();
			if (i % persist_factor == 0)
				persist[persist_idx++] = obj;
		}
	}

	static void Main (string[] args) {
		if (args.Length > 0)
			loops = int.Parse (args [0]);
		if (args.Length > 1)
			count = int.Parse (args [1]);
		if (args.Length > 2)
			persist_factor = int.Parse (args [2]);
		for (int i = 0; i < loops; ++i) {
			work ();
		}
	}
}
