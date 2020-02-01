using System;

namespace T {

	public class Test {

		public static int Main () {
			int i = 12;
			object o = i;
			
			if (i.ToString () != "12")
				return 1;
			if (((Int32)o).ToString () != "12")
				return 2;
			if (o.ToString () != "12")
				return 3;
			return 0;
		}
	}
}
