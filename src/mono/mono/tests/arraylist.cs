using System.Collections;

namespace Test {
	public class Test {
		public static int Main() {
			ArrayList a = new ArrayList (10);
			int i = 0;
			a.Add (0);
			a.Add (1);
			a.Add (2);
			a.Add (3);
			foreach (int elem in a) {
				if (elem != i++)
					return i;
			}
			return 0;
		}
	}
}
