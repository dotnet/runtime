using System;

struct Foo<T> {
	public T t;
	public Foo (T t) { this.t = t; }
}

/*This is a WB-stress based test */

public class Driver<T> {
	public static void Fill (int cycles, Func<int, T> mk_entry) {
		const int array_len = 9975;
		Foo<T>[] root = new Foo<T> [array_len];
		for (int i = 0; i < cycles; ++i) {
			for (int j = 0; j < array_len; ++j)
				root [j] = new Foo<T> (mk_entry (j));
		}

		for (int i = 0; i < array_len; ++i)
			if (root [i].Equals (mk_entry (i)))
				throw new Exception ("Invalid value at position " + i);
	}	
}

class Driver {

	static void Main () {
		int loops = 40;
		int cycles = 40;
		for (int i = 0; i < loops; ++i) {
			Driver<object>.Fill (cycles, (k) => k);
		}
	}
}

