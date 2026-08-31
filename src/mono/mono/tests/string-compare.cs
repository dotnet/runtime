using System;

namespace T {
	struct datum {
		public string a;
		public string b;
		public int result;

		public datum (string A, string B, int r) {
			a = A;
			b = B;
			result =r;
		}

		public bool match () {
			int r = String.Compare (a, b);
			switch (result) {
			case -1:
				if (r < 0) return true;
				break;
			case 0:
				if (r == 0) return true;
				break;
			case 1:
				if (r > 0) return true;
				break;
			default:
				return false;
			}
			return false;
		}
	}
	public class test {
		public static int Main() {
			datum[] data = {
				new datum ("a", "b", -1),
				new datum ("a", "a", 0),
				new datum ("b", "a", 1),
				new datum ("ba", "b", 1),
				new datum ("b", "ba", -1),
			};
			int i;
			for (i = 0; i < data.Length; ++i) {
				if (!data[i].match())
					return i+1;
			}
			return 0;
		}
	}
}
