using System;

class Test {
	
	private long res = 0;

	void test ()
	{
		long a = 1, b = 2;

		res = 2 * a + 3 * b;
	}
	
	static int Main ()
	{
		Test x = new Test ();

		x.test ();

		Console.WriteLine (x.res);

		if (x.res != 8)
			return 1;
		
		return 0;
	}
}
