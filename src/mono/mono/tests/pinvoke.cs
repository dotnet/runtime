using System;
using System.Runtime.InteropServices;

public class Test {

	public static int Main () {
		if (Math.Cos (Math.PI) != -1)
			return 1;
		if (Math.Acos (1) != 0)
			return 1;
		
		return 0;
	}
}


