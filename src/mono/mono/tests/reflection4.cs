using System;
using System.Reflection;

public class T {

	public static int Main() {
		Type t = typeof (System.Console);
		Type[] p= {typeof(string)};
		
		MethodInfo m = t.GetMethod ("WriteLine", p);

		if (typeof(void) != m.ReturnType) {
			Console.WriteLine ("Type mismatch");
			return 1;
		}
		return 0;
	}
}
