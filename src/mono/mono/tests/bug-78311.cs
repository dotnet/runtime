using System;
using System.Threading;

public class Test {
	public static int Main() {
		int test = 1;
		int result = Interlocked.Increment(ref test);

		if (result != 2) {
			Console.WriteLine("Incorrect Increment result: " + result);
			return 1;
		} else {
			return 0;
		}
	}
}
