// Foo() benefits from definite assignment analysis

using System;

class Test {
	static void Foo () {
		int a, b, c, d, e, f, g;
		int h, i, j, k, l, m, n;

		for (int x = 0; x < 2; ++x) {
			a = 0;
			b = a;
			c = a;
			d = b;
			e = c;
			f = 1;
			g = 2;
			h = a + b;
			i = h + h;
			j = 1 + b + c;
			k = i + j;
			l = f + g;
			m = k + l;
			n = l + l;
		}
	}
		

	static void Main () {
		for (int i = 0; i < 100000000; ++ i)
			Foo ();
	}
}
