public class Switch {

	public static int test (int n) {
		switch (n) {
		case 0: return 1;
		case 1: return 0;
		case -1: return 2;
		default:
			return 0xff;
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
		return 0;
	}
}


