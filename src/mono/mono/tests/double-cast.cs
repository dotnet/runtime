using System;

namespace System  {
	public class Test {
		public static int Main() {
			double d = 5.0;
			object o = d;
			double e = (double) o;

			if (e != d)
				return 1;
			return 0;
		}
	}
}
