using System;

public struct A {
	public int a;
	static int Main ()
	{
		A a = new A ();
		A b = new A ();
		
		for (int i = 0; i < 50000000; i++) {
			a.a = i;
			b.a = i + 5;

			a.a = Foo (a, b);
			b.a = a.a + 8;
			
			Foo (a, b);
		}
		
		return 0;
	}
	
	static int Foo (A a, A b) {
		return a.a + b.a;
	}
}