using System;

public class MyEx : Exception {
	public MyEx () {}
}

public class Ex {

	public static int test (int a) {
		int res;
		int fin = 0;
		try {
			res = 10/a;
			throw new MyEx ();
		} catch (Exception ex) {
			ex = new MyEx ();
			throw;
		} finally {
			fin = 1;
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
		try {
			test (0);
		} catch (MyEx ex) {
			catched = 2;
		} catch {
			// we should get here because rethrow rethrows the dividebyzero ex
			// not the exception assigned to the local var (a MyEx)
			catched = 3;
		}
		if (catched != 3)
			return 3;
		return 0;
	}
}


