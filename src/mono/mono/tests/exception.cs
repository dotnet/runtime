using System;

public class Ex {

	public static int test (int a) {
		int res;
		try {
			res = 10/a;
		} catch (DivideByZeroException ex) {
			res = 3;
		} catch {
			res = 2;
		} finally {
			res = 0;
		}
		return res;
	}
	public static int Main () {
		return test (0);
	}
}


