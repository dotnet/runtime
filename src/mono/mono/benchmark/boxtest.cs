using System;


public class Test {

	public static int Main (string[] args) {
		object x = null;
		
		for (int i = 0 ; i < 5000000; i++) {
			x = i;
		}

		int j = (int)x;
		
		return 0;
	}
}


