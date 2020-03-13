using System;

class X {

	public static int DivUn(int x) {
		x *= 163859;
		x = (int) ((uint)x / 5);
		x = (int) ((uint)x / 25);
		x = (int) ((uint)x / 10);
		x = (int) ((uint)x / 128);
		x = (int) ((uint)x / 43);
		x = (int) ((uint)x / 2);
		x = (int) ((uint)x / 4);
		x = (int) ((uint)x / 1);
		return x;
	}

	public static int Main() {
		int x = 1;
		for (int i=0; i < 100000000; ++i) x += DivUn(12345);
		// x |= -1; // check for overflow case
		x = (int) ((uint)x / 1025);		
		return x;
	}

}
