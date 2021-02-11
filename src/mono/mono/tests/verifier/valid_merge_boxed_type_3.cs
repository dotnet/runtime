using System;
using System.Reflection;
using System.Reflection.Emit;

class Driver {
	public static int Main (string[] args) {
		object[] o = new object[10];
		o[5] = args == null ? (object)1 :  "oi";
		return 1;
	}
}
