//
// You need a deadce to get rid of the initlocals statements,
// which make all of the variables overlap in their live ranges.
//

using System;
class T {
	static void Main () {
		int i = Environment.TickCount;
		new T ().X ();
		Console.WriteLine (Environment.TickCount - i);
	}
	
	void X () {
		int a = 0;
		for (int x = 0; x < 1000; x ++) {
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
			for (int i = 0; i < 10000; i ++) a ++;
		}
	}
}