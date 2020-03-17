using System;

class Test {
	public static bool testprime (int x) {
		if ((x & 1) != 0) {
			for (int n = 3; n < x; n += 2) {
				if ((x % n) == 0)
					return false;
			}
			return true;
		}
		return (x == 2);
	}

	public static int Main () {
		if (!testprime (17))
			return 1;
		return 0;
	}
}
