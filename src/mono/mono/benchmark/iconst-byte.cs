using System;

class A {
	static int Main ()
	{
		// prevent ssa (for now)
		int dummy;
		Foo (out dummy);
		
		for (int i = 0; i < 50000000; i++) {
			byte b;
			
			b = 0;
			b = 0;
			b = 0;
			b = 0;
			b = 0;
			b = 0;
			b = 0;
			b = 0;
			b = 0;
			b = 0;
			b = 0;
			b = 0;
			b = 0;
			b = 0;
			b = 0;
			b = 0;
			b = 0;
			b = 0;
			b = 0;
			b = 0;
			b = 0;
			b = 0;
		}
		
		return 0;
	}
	
	static void Foo (out int dummy) { dummy = 0; }
}