using System;
using System.IO;
using System.Reflection;

public class Test {
	public static int Main ()
	{
		var p = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "middle", "Mid.dll");
		var a = Assembly.LoadFile (p);
		var t = a.GetType ("MyType");
		bool caught = false;
		try {
			Activator.CreateInstance (t);
		} catch (TargetInvocationException tie) {
			if (tie.InnerException is FileNotFoundException) {
				/* reference assembly loading throws FNFE */
				caught = true;
			} else {
				Console.Error.WriteLine ($"Expected tie.InnerException to be FileNotFoundException, but got {tie.InnerException}");
				return 1;
			}
		} catch (Exception e) {
			Console.Error.WriteLine ($"Expected TargetInvocationException, but got {e}");
			return 2;
		}
		if (!caught) {
			Console.Error.WriteLine ($"Expected an exception, but got none");
			return 3;
		}
		return 0;
	}
}
