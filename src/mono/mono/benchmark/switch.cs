public class Tests {

	public static int Main () {
		int n = 2;
		int b = 0;

		for (int i = 0; i < 1000000000; i++) {
			switch (n) {
			case 0: b = 2; break;
			case 1: b = 3; break;
			case 2: b = 4; break;
			case -1: b = 5; break;
			default:
				b = 6;
				break;
			}
		}

		if (b != 4)
			return 1;
		
		return 0;
	}
}


