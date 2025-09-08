using System;
using System.Reflection;
using System.IO;

class Driver {
	static int Main () {
		var src = Path.GetDirectoryName (typeof (Driver).Assembly.Location);
		var dep_asm = Assembly.UnsafeLoadFrom (Path.Combine (src, "reflection-load-with-context-lib.dll"));
		var type = dep_asm.GetType ("B.ClassB");
		var attr_type = dep_asm.GetType ("B.MyAttribute");

		try {
			Activator.CreateInstance (type);
		} catch (Exception) {
			return 1;
		}

		try {
			type.GetCustomAttributes (attr_type, false);
		} catch (Exception) {
			return 2;
		}
		return 0;
	}
}

