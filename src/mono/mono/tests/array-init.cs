using System;

namespace Test {
	public class Test {
		private static int[] array = {0, 1, 2, 3};
		public static int Main() {
			int t = 0;
			foreach (int i in array) {
				if (i != t++)
					return 1;
			}
			return 0;
		}
	}
}
