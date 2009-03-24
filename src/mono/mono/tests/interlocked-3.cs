using System;
using System.Threading;

public class main {
	public static int Main () {
		int n2 = 0, n1 = 1, n, i;
		n = 0;
		i = 0;
		while (i < 10) {
			//n = n2 + n1;
			Interlocked.CompareExchange (ref n, n2 + n1, n);
			//n2 = n1;
			Interlocked.CompareExchange (ref n2, n1, n2);
			//n1 = n;
			Interlocked.CompareExchange (ref n1, n, n1);
			//i = i + 1;
			Interlocked.CompareExchange (ref i, i + 1, i);
		}
		if (n != 89)
			return 1;
		return 0;
	}
}
