using System;
using System.IO;
using System.Reflection;

class App
{
	public static int Main ()
	{
		AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler (MyResolveEventHandler);

		try {
			var a = Assembly.Load ("test");
			foreach (Type t in a.GetTypes ()) {
				Console.WriteLine ("pp: " + t + " " + t.BaseType);
			}
		} catch (Exception ex) {
			Console.WriteLine ("Caught exception: {0}", ex);
			return 1;
		}

		return 0;
	}

	static Assembly MyResolveEventHandler (object sender, ResolveEventArgs args)
	{
		var path = Path.Combine (Directory.GetCurrentDirectory (), "assemblyresolve", "deps");
		if (args.Name == "test" && args.RequestingAssembly == null)
			return Assembly.LoadFile (Path.Combine (path, "test.dll"));
		if (args.Name == "TestBase, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" && args.RequestingAssembly.GetName ().Name == "test")
			return Assembly.LoadFile (Path.Combine (path, "TestBase.dll"));

		throw new InvalidOperationException (String.Format ("Unexpected parameter combination {0} {1}", args.Name, args.RequestingAssembly));
	}
}
