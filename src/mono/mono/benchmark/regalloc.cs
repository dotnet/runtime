//
// To do this test well, I think you need to move the int aX = 0...
// lines down as far as possible. That way, the lifespan of the variables
// is short, and they can go into registers.
//

class T {
	static int Main ()
	{
		for (int r = 0; r < 50; r ++) {
			int a0 = 0, b0 = 0, c0 = 0, d0 = 0;
			int a1 = 0, b1 = 0, c1 = 0, d1 = 0;
			int a2 = 0, b2 = 0, c2 = 0, d2 = 0;
			int a3 = 0, b3 = 0, c3 = 0, d3 = 0;
			int a4 = 0, b4 = 0, c4 = 0, d4 = 0;
			
			int x = 0;
			
			for (int i = 0; i < 400000; i ++) a0 ++;
			for (int i = 0; i < 400000; i ++) b0 ++;
			for (int i = 0; i < 400000; i ++) c0 ++;
			for (int i = 0; i < 400000; i ++) d0 ++;
			x ^= a0 ^ b0 ^ c0 ^ d0;
			
			for (int i = 0; i < 400000; i ++) a1 ++;
			for (int i = 0; i < 400000; i ++) b1 ++;
			for (int i = 0; i < 400000; i ++) c1 ++;
			for (int i = 0; i < 400000; i ++) d1 ++;
			x ^= a1 ^ b1 ^ c1 ^ d1;
			
			for (int i = 0; i < 400000; i ++) a2 ++;
			for (int i = 0; i < 400000; i ++) b2 ++;
			for (int i = 0; i < 400000; i ++) c2 ++;
			for (int i = 0; i < 400000; i ++) d2 ++;
			x ^= a2 ^ b2 ^ c2 ^ d2;
			
			for (int i = 0; i < 400000; i ++) a3 ++;
			for (int i = 0; i < 400000; i ++) b3 ++;
			for (int i = 0; i < 400000; i ++) c3 ++;
			for (int i = 0; i < 400000; i ++) d3 ++;
			x ^= a3 ^ b3 ^ c3 ^ d3;
			
			for (int i = 0; i < 400000; i ++) a4 ++;
			for (int i = 0; i < 400000; i ++) b4 ++;
			for (int i = 0; i < 400000; i ++) c4 ++;
			for (int i = 0; i < 400000; i ++) d4 ++;
			x ^= a4 ^ b4 ^ c4 ^ d4;
			
			if (x != 0)
				return 1;
		}
		return 0;
	}
}