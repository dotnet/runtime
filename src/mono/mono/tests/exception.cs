using System;

public class Ex {

	public static int test (int a) {
		int res;
		int fin = 0;
		try {
			res = 10/a;
		} catch (DivideByZeroException ex) {
			if (fin != 1)
				res = 34;
			else
				res = 33;
		} catch {
			if (fin != 1)
				res = 24;
			else
				res = 22;
		} finally {
			fin = 1;
		}
		return res;
	}
	public static int Main () {
		if (test(1) != 10)
			return 1;
		if (test(0) != 34)
			return 2;
		return 0;
	}
}


