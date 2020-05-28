using System;
using System.IO;
using System.Reflection;

class App
{
	const int expected_count = 2;
	static int event_handler_count;
	
	public static int Main ()
	{
		AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler (MyResolveEventHandler);
		
		event_handler_count = 0;
		try {
			Assembly a = Assembly.LoadFile (Path.Combine (Directory.GetCurrentDirectory (), "assemblyresolve_asm.dll"));
			foreach (Type t in a.GetTypes ()) {
				Console.WriteLine ("pp: " + t + " " + t.BaseType);
			}
		} catch (Exception ex) {
			Console.WriteLine ($"Caught exception: {ex}");
			return 1;
		}
		
		if (event_handler_count != expected_count) {
			Console.WriteLine ($"Expected MyResolveEventHandler to be called {expected_count} but was called {event_handler_count}");
			return 2;
		}
				
		return 0;
	}
	
	static Assembly MyResolveEventHandler (object sender, ResolveEventArgs args)
	{
		event_handler_count++;
		Console.WriteLine ("Resolve assembly: {0}", args.Name);
		if (args.Name == "Test, Version=0.0.0.0, Culture=neutral" || args.Name == "Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
			return Assembly.LoadFile (Path.Combine (Directory.GetCurrentDirectory (), "assemblyresolve_deps", "Test.dll"));
		if (args.Name == "TestBase, Version=0.0.0.0, Culture=neutral" || args.Name == "TestBase, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
			return Assembly.LoadFile (Path.Combine (Directory.GetCurrentDirectory (), "assemblyresolve_deps", "TestBase.dll"));
		return null;
	}
}
