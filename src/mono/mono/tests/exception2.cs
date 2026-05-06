using System;

public class Ex2 {

	public static int test (int a) {
		int res;
		res = 10/a;
		return res;
	}
	public static int Main () {
		int res = 1;
		try {
			test(1);
			test(0);
		} catch {
			res = 0;
		}
		return res;
	}
}


