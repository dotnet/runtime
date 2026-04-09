using System;

public class Ex {

	public static int test (int a) {
		int res;
		int fin = 0;
		try {
			try {
				res = 10/a;
			} catch (DivideByZeroException ex) {
				res = 34;
			} catch {
				res = 22;
			} 
		} catch {
			res = 44;
		} finally {
			fin += 1;
		}
		return fin;
	}
	public static int Main () {
		if (test(0) != 1)
			return 1;
		return 0;
	}
}


