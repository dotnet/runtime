using System;

public class X {
	int a;
}

public class Test {

	struct test {
		public int v1;
	}
	public static int Main () {

		test a = new test();

		a.v1 = 5;
		
		Console.WriteLine (a.GetHashCode ());

		X b = new X();
		X c = new X();
		
		Console.WriteLine (b.GetHashCode ());
		Console.WriteLine (c.GetHashCode ());
		
		return 0;
	}
}
