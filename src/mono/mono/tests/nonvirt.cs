using System;

public class TestObj {

	public int amethod () {
		return 1;
	}
	
	public static int Main () {
		TestObj obj = null;

		try {
			obj.amethod ();
		} catch (NullReferenceException) {
			return 0;
		}
		
		return 1;
	}
}


