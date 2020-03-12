using System;

public class Ex {

	public static int Main () {

		try {
			throw null;
		} catch {
			return 0;
		}
		return 1;
	}
}


