using System;
class T {
	static void Main () {
		int a = 1, b = 2, c = 3, d = 4, e = 5;
		for (int i = 0; i < 500000000; i ++) {
			// on the stack
			a = e == 1 ? b : c;
			b = a == 1 ? c : d;
			c = b == 1 ? d : e;
			d = c == 1 ? e : a;
			e = d == 1 ? a : b;
		}
	}
	
}