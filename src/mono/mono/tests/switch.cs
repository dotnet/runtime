public class Switch {

	public static int test (int n) {
		switch (n) {
		case 0: return 1;
		case 1: return 0;
		case -1: return 2;
		default:
			return 0xff;
		}
		return 100;
	}

	const string long_str =
	"{http://schemas.xmlsoap.org/ws/2003/03/business-process/}partnerLinks";

	// bug #54473
	public static int test_string (string s) {
		switch (s) {
		case "{http://schemas.xmlsoap.org/ws/2003/03/business-process/}partnerLinks":
			return 1;
		default:
			return 0;
		}
	}

	public static int Main () {
		if (test(0) != 1)
			return 1;
		if (test(1) != 0)
			return 2;
		if (test(-1) != 2)
			return 3;
		if (test(3) != 0xff)
			return 4;
		if (test_string (long_str) != 1)
			return 5;
		return 0;
	}
}


