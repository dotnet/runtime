using System;
class T {
	// test x ? A : B where A and B are constants.
	static void Main () {
		int a = 0, b = 0, c = 0, d = 0, e = 0;
		for (int i = 0; i < 50000000; i ++) {			
			a = b == 10 ?  1 :  1;
			b = b >  1  ?  9 :  8;
			c = b <= c  ?  1 :  2;
			d = d >  0  ?  1 :  0;
			e = e == 0  ? -1 :  0;
		}
	}
}