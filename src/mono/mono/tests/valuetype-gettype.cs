using System;

namespace Test {
	struct val {
		int t;

		val (int v) {
			t = v;
		}

		public static int Main() {
			val v = new val (1);;
			Console.WriteLine (v.ToString());
			return 0;
		}
	}
}
