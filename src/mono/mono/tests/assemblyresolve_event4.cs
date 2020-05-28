using System;
using System.IO;
using System.Reflection;

class App
{
	public static int Main ()
	{
		AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler (MyResolveEventHandler);

		try {
			var a = Assembly.Load ("Test");
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
		if (args.Name == "Test" && args.RequestingAssembly == null)
			return Assembly.LoadFile (Path.Combine (Directory.GetCurrentDirectory (), "assemblyresolve_deps", "Test.dll"));
		if (args.Name == "TestBase, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" && args.RequestingAssembly.GetName ().Name == "Test")
			return Assembly.LoadFile (Path.Combine (Directory.GetCurrentDirectory (), "assemblyresolve_deps", "TestBase.dll"));

		throw new InvalidOperationException (String.Format ("Unexpected parameter combination Name=\"{0}\" RequestingAssembly=\"{1}\"", args.Name, args.RequestingAssembly));
	}
}
