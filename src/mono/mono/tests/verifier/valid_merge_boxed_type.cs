using System;
using System.Reflection;
using System.Reflection.Emit;

class Driver {
	public static int Main (string[] args) {
		object o;
		o = args == null ? new object () : (object)1;
		return 1;
	}
}
