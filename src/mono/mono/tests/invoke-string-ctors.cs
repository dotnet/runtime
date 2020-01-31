using System;
using System.Reflection;

class T {

	const int count = 10000;
	static int Main () {
		int res, i;
		for (i = 0; i < count; ++i) {
			res = run ();
			if (res != 0)
				return res;
		}
		return 0;
	}

	static unsafe int run () {
		char[] val = new char[] {'h', 'e', 'l', 'l', 'o'};
		string a;

		a = (string)Activator.CreateInstance (typeof (string), new object[] {'a', 5});
		if (a != "aaaaa") {
			return 1;
		}
		a = (string)Activator.CreateInstance (typeof (string), new object[] {val});
		if (a != "hello") {
			return 2;
		}
		a = (string)Activator.CreateInstance (typeof (string), new object[] {val, 0, 3});
		if (a != "hel") {
			return 3;
		}
		/*
		 * The other ctors use pointers: maybe something like this is supposed to work some day.
		fixed (char *c = val) {
			a = (string)Activator.CreateInstance (typeof (string), new object[] {Pointer.Box (c, typeof (char*))});
			if (a != "hello") {
				return 4;
			}
		}*/
		return 0;
	}
}

