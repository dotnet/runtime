using System;

public class Ex {

	public static int test () {
		int ocount = 0;
		
		checked {

			ocount = 0;
			try {
				sbyte a = 126;
				sbyte t = a++;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 1;

			ocount = 0;
			try {
				sbyte a = 127;
				sbyte t = a++;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 1;

			ocount = 0;
			try {
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 1;

			ocount = 0;
			try {
				int a = 1 << 29;
				int t = a*2;
			} catch {
				ocount++;
			}
			if (ocount != 0)
				return 1;

			ocount = 0;
			try {
				int a = 1 << 30;
				int t = a*2;
			} catch {
				ocount++;
			}
			if (ocount != 1)
				return 1;
		
		}
		
		return 0;
	}
	public static int Main () {
		return test();
	}
}


