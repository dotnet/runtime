//
// bug-27420.cs: Using valuetypes in a loop leads to crash
//

using System;

struct A1 {
	int i, j, k, l, m, n, o, p;
}

// Allocate a big structure
struct A2 {
	A1 a, b, c, d, e, f;

	public int g;
}

public class crash
{
	static A2 get_a2 () {
		return new A2 ();
	}

	static void Main() {
		int i;

		for (int j = 0; j < 100000; ++j) {
			// Force the runtime to create a temporary valuetype on the stack
			i = get_a2 ().g;
		}
	}
}
