using System;
class T {
	static void Main () {
		int i = Environment.TickCount;
		new T ().X ();
		Console.WriteLine (Environment.TickCount - i);
	}
	
	void X () {
		long a = 1, b = 2, c = 3, d = 4;
		
		for (int i = 0; i < 10000000; i ++) {
			a /= (b + 1);
			b /= (c + 1);
			c /= (d + 1);
			d /= (a + 1);
			
			a *= (b + 2);
			b *= (c + 2);
			c *= (d + 2);
			d *= (a + 2);
		}
	}
}