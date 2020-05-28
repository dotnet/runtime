using System;

class T {
	T next;

	static void Main (string[] args) {
		int count = 5000;
		T list = null;
		for (int i = 0; i < count; ++i) {
			T n = new T ();
			n.next = list;
			list = n;
		}
		// trigger a heapshot
		GC.Collect ();
		for (int i = 0; i < 23; ++i) {
			T n = new T ();
			n.next = list;
			list = n;
		}
		// trigger another heapshot
		GC.Collect ();
	}
}

