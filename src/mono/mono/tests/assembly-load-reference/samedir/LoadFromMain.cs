using System;
using System.IO;
using System.Reflection;

public class Test {
	public static int Main ()
	{
		var p = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "Mid.dll");
		var a = Assembly.LoadFrom (p);
		var t = a.GetType ("MyType");
		Activator.CreateInstance (t);
		return 0;
	}
}
