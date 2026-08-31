using System;

// You need to compile this test with mcs:
// csc will discard unreachable code sections

namespace Test {
	public class Test {
		public static int Main () {
			int var = 0;
			goto label2;
			label1:
			goto label2;
			label2:
			return var;
		}
	}
}
