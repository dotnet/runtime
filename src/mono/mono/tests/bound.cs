using System;

public class Bound {
	public static int Main () {

		try {
			
			byte [] b = new byte [0];
			b [0] = 128;
			return 1;
		} catch {
			return 0;
		}
		return 0;
	}
}

