using System;
using System.Threading;

// Regression test for bug #59299

public class Test
{
	delegate void M(ref object x, ref object y);
	
	public static void Foo(ref object x, ref object y)
	{
		x = 20;
		y = 30;
	}
	
	public static int Main()
	{
		IAsyncResult ar;
		M m = new M(Foo);
		object x1 = 10, x2 = 10;

		ar = m.BeginInvoke(ref x1, ref x2, null, null);
		
		m.EndInvoke(ref x1, ref x2, ar);
		
		if ((int)x1 != 20 || (int)x2 != 30)
			return 1;
			
		return 0;
	}
}
