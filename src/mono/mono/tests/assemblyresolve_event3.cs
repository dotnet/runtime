using System;
using System.IO;
using System.Reflection;

class App
{
	const int expected_count = 1;
	static int event_handler_count;
	
	public static int Main ()
	{
		AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler (MyResolveEventHandler);
		
		event_handler_count = 0;
		try {
			Assembly a = Assembly.LoadFile (String.Format ("{0}{1}assemblyresolve{1}test{1}asm.dll", Directory.GetCurrentDirectory (), Path.DirectorySeparatorChar));
			foreach (Type t in a.GetTypes ()) {
				Console.WriteLine ("pp: " + t + " " + t.BaseType);
			}
		} catch (Exception ex) {
			Console.WriteLine ("Caught exception: {0}", ex.Message);
			return 1;
		}
		
		if (event_handler_count != expected_count)
			return 2;
				
		return 0;
	}
	
	static Assembly MyResolveEventHandler (object sender, ResolveEventArgs args)
	{
		event_handler_count++;
		Console.WriteLine ("Resolve assembly: {0}", args.Name);
		if (args.Name == "test, Version=0.0.0.0, Culture=neutral" || args.Name == "test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
			return Assembly.LoadFile (String.Format ("{0}{1}assemblyresolve{1}deps{1}test.dll", Directory.GetCurrentDirectory (), Path.DirectorySeparatorChar));
		if (args.Name == "TestBase, Version=0.0.0.0, Culture=neutral" || args.Name == "TestBase, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
			return Assembly.LoadFile (String.Format ("{0}{1}assemblyresolve{1}deps{1}TestBase.dll", Directory.GetCurrentDirectory (), Path.DirectorySeparatorChar));
		return null;
	}
}
