using System;

class T {

	static int count = 1000000;
	static void Main (string[] args) {
		if (args.Length > 0)
			count = int.Parse (args [0]);
		for (int i = 0; i < count; ++i) {
			object o = new object ();
		}
	}
}

