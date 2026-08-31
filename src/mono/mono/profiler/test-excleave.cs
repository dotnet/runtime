using System;

class T {

	static int f = 0;
	static int c = 0;
	static void throw_ex () {
		try {
			throw new Exception ();
		} finally {
			f++;
		}
	}
	static void Main (string[] args) {
		for (int i = 0; i < 1000; ++i) {
			try {
				throw_ex ();
			} catch {
				c++;
			}
		}
	}
}

