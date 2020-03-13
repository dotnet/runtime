using System;
class T {
	static void Main () {
		int a = 1, b = 2, c = 3, d = 4, e = 5;
		for (int i = 0; i < 500000000; i ++) {
			// on the stack
			if (a == b)
				a = i;
			if (b == a)
				b = i;
			if (c == d)
				c = i;
			if (d == e)
				d = i;
			if (e == a)
				e = i;
		}
		
		if ((a ^ b ^ c ^ d ^ e) == 12345)
			return;
	}
	
}