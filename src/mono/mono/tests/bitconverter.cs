using System;

namespace Test {

	public class Test {
		public static int Main() {
			byte[] rep;
			double d = 5.0;

			rep = BitConverter.GetBytes (d);
			double res = BitConverter.ToDouble (rep, 0);
#if DEBUG
			Console.WriteLine ("{0} {1} {2} {3} {4} {5} {6} {7}",
				rep [0], rep [1], rep [2], rep [3], rep [4],
				rep [5], rep [6], rep [7]);
#endif
			if (d != res)
				return 1;
			return 0;
		}
	}
}
