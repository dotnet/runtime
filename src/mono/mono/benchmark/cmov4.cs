using System;
class T {
	// some more advanced versions.
	static void Main () {
		int a = 0, b = 0, c = 0, d = 0, e = 0;
		for (int i = 0; i < 50000000; i ++) {
			// sgn (x)
			if (a == 0)
				a = 0;
			else if (a < 0)
				a = -1;
			else
				a = 1;
			
			// cond incr
			if (a <= 0)
				a ++;
			
			// buffer ring
			if (b == 49)
				b = 0;
			else
				b ++;
			
			// max
			c = a > b ? a : b;
			
			// abs
			d = a > 0 ? a : -a;
		}
	}
	
}