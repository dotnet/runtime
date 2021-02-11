using System;
using System.Reflection;

class T {
	public static unsafe void meth (int a, int* b) {
	}
	static int Main () {
		ParameterInfo[] args = typeof (T).GetMethod ("meth").GetParameters ();
		if (args[0].ParameterType == args[1].ParameterType)
			return 1;

		unsafe { 
			if (typeof(int) == typeof (int*))
				return 2;
		}
		if (args[0].ParameterType != typeof(int))
			return 3;

		unsafe { 
			if (args[1].ParameterType != typeof(int*))
				return 4;
		}

		return 0;
	}
}
