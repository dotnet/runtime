using System;

class Test {

	static double test ()
	{
		return double.NaN;
	}
	
	static int Main()
	{
		ulong u = 3960077;
		ulong f = 1000000;

		for (int i = 0; i < 100; i++)
			test ();
		
		double d = u/(double)f;

		if (d != 3.960077)
			return 1;

		return 0;	
	}
}

