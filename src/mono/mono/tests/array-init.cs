using System;

namespace Test {
	public class Test {
		private static int[] array = {0, 1, 2, 3};
		private static int [,] bar = { {0,1}, {4,5}, {10,20} };

		public static int Main() {		
			int num = 1;
			int t = 0;
			foreach (int i in array) {
				if (i != t++)
					return num;
			}

			num++;			
			if (bar [0,0] != 0)
				return num;
			num++;
			if (bar [0,1] != 1)
				return num;
			num++;
			if (bar [1,0] != 4)
				return num;
			num++;
			if (bar [1,1] != 5)
				return num;
			num++;
			if (bar [2,0] != 10)
				return num;
			num++;
			if (bar [2,1] != 20)
				return num;

			num++;
			
			short [,] j = new short [4,2] { {0,1}, {2,3}, {4,5}, {6,7} };
			if (j [1,1] != 3)
				return num;

			return 0;
		}
	}
}
