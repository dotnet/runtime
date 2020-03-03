using System;
using System.Reflection;

public class App
{
	static bool[] expected_results = {true, false, false, true};
	static bool handler_fired;
	
	public static int Main ()
	{
		AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(MyResolveEventHandler);

		int i = 0;
		handler_fired = false;
		
		try {
			Assembly.Load ("SomeAssembly");
		} catch (Exception) {
		}
		if (expected_results [i] != handler_fired)
			return 1;

		i++;
		handler_fired = false;
		try {
			Assembly.LoadFile ("SomeAssembly");
		} catch (Exception) {
		}
		if (expected_results [i] != handler_fired)
			return 2;
		
		i++;
		handler_fired = false;
		try {
			Assembly.LoadFrom ("SomeAssembly");
		} catch (Exception) {
		}
		if (expected_results [i] != handler_fired)
			return 3;

		i++;
		handler_fired = false;
		try {
			Assembly.LoadWithPartialName ("SomeAssembly");
		} catch (Exception) {
		}
		if (expected_results [i] != handler_fired)
			return 4;

		return 0;
	}

	static Assembly MyResolveEventHandler(object sender, ResolveEventArgs args) {
		handler_fired = true;
		return null;
	}
}
