using System;

public class Test {

	public static int test_0_liveness_exception() {
		int id = 1;

		try {
			id = 2;
			throw new Exception ();
		}
		catch (Exception) {
			if (id != 2)
				return id;
		}

		return 0;
	}

	public static int Main() {
		int res = 0;

		res = test_0_liveness_exception ();
		if (res != 0)
			Console.WriteLine ("error, test_0_liveness_exception res={0}", res);
		
		return 0;
	}
}
