// Since the structs are readonly, the expression here is a jit time constant.

using System;

struct A {
	struct B { int dummy; public C c; }
	struct C { int dummy; public D d; }
	struct D { public int i; }
	
	static readonly B b, bb;
	
	static int Main ()
	{
	
		for (int i = 0; i < 50000000; i++) {
			if (b.c.d.i != bb.c.d.i)
				return 1;
		}
		
		return 0;
	}
}