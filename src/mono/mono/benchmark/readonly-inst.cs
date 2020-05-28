using System;

class A {
	class B { public readonly C c = new C (); }
	class C { public readonly D d = new D (); }
	class D { public readonly E e = new E (); }
	class E { public readonly int i = 1; }
	
	static readonly A foo = new A ();
	static readonly A bar = new A ();
	
	readonly B b = new B ();
	static int Main ()
	{
	
		for (int i = 0; i < 50000000; i++) {
			if (foo.b.c.d.e.i != bar.b.c.d.e.i)
				return 1;
		}
		
		return 0;
	}
}