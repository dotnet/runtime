using System;
using System.Runtime.CompilerServices;

class T {

	static object o = null;

	[MethodImpl (MethodImplOptions.NoInlining)]
	static void level3 (int op) {
		level2 (op);
	}

	[MethodImpl (MethodImplOptions.NoInlining)]
	static void level2 (int op) {
		level1 (op);
	}

	[MethodImpl (MethodImplOptions.NoInlining)]
	static void level1 (int op) {
		level0 (op);
	}

	[MethodImpl (MethodImplOptions.NoInlining)]
	static void level0 (int op) {
		switch (op) {
		case 0: o = new T (); break;
		case 1: throw new Exception (); break;
		}
	}

	static void Main (string[] args) {
		int count = 1010;
		for (int i = 0; i < count; ++i) {
			level3 (0);
		}
		for (int i = 0; i < count; ++i) {
			try {
				level3 (1);
			} catch {
			}
		}
	}
}

