using System.Collections;

namespace Test {
	public class Test {
		public static int Main () {
			string[] names = {
				"one", "two", "three", "four"
			};
			Hashtable hash = new Hashtable ();

			for (int i=0; i < names.Length; ++i) {
				hash.Add (names [i], i);
			}
			if ((int)hash ["one"] != 0)
				return 1;
			if ((int)hash ["two"] != 1)
				return 2;
			if ((int)hash ["three"] != 2)
				return 3;
			if ((int)hash ["four"] != 3)
				return 4;
			if (hash.Contains("urka"))
				return 5;
			return 0;
		}
	}
}
