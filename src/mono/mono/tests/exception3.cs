using System;

public class MyEx : Exception {
	public MyEx () {}
}

public class Ex {

	static int fin;

	public static int test (int a) {
		int res;

		fin = 0;

		try {
			try {
				res = 10/a;
				throw new MyEx ();
			} catch (DivideByZeroException ex) {
				if (fin != 1)
					res = 34;
				else
					res = 33;
			} finally {
				fin = 1;
			}
		} finally {
			fin += 1;
		}
		
		return res;
	}
	public static int Main () {
		int catched = 0;
		try {
			test (1);
		} catch (MyEx ex) {
			catched = 1;
		}
		if (catched != 1)
			return 2;

		if (fin != 2)
			return 3;
		
		if (test(0) != 34)
			return 4;
		return 0;
	}
}


