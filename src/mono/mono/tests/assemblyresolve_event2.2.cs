using System;
using System.Reflection;

public class App
{
	static bool[] expected_results = {false, false};
	static bool handler_fired;
	
	public static int Main ()
	{
		AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += new ResolveEventHandler(MyReflectionResolveEventHandler);

		int i = 0;
		handler_fired = false;

		try {
			Assembly.ReflectionOnlyLoad ("SomeAssembly");
		} catch (Exception) {
		}
		if (expected_results [i] != handler_fired)
			return 1;

		i++;
		handler_fired = false;
		try {
			Assembly.ReflectionOnlyLoadFrom ("SomeAssembly");
		} catch (Exception) {
		}
		if (expected_results [i] != handler_fired)
			return 2;

		return 0;
	}

	static Assembly MyReflectionResolveEventHandler(object sender, ResolveEventArgs args) {
		handler_fired = true;
		return null;
	}
}
