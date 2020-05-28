using System;
using System.Reflection;

public class App {
	const string assemblyName = "gactestlib";
	const string assemblyVersion = "1.0.0.0";
	const string assemblyPublicKeyToken = "537eab56aa911cb7"; /* see testkey.snk */
	public static int Main (string[] args)
	{
		TestAssemblyLoad ();

		TestReflectionOnlyLoad ();

		return 0;
	}

	public static void TestAssemblyLoad ()
	{
		var expectedVersion = new Version (assemblyVersion);

		var s = String.Format ("{0}, Version={1}, Culture=\"\", PublicKeyToken={2}",
				       assemblyName, assemblyVersion, assemblyPublicKeyToken);
		var n = new AssemblyName (s);
		var a = AppDomain.CurrentDomain.Load (n);

		if (a == null)
			Environment.Exit (1);
		if (a.GetName ().Version != expectedVersion)
			Environment.Exit (2);
	}

	public static void TestReflectionOnlyLoad ()
	{
		var expectedVersion = new Version (assemblyVersion);

		var s = String.Format ("{0}, Version={1}, Culture=\"\", PublicKeyToken={2}",
				       assemblyName, assemblyVersion, assemblyPublicKeyToken);
		var a = Assembly.ReflectionOnlyLoad (s);

		if (a == null)
			Environment.Exit (3);
		if (a.GetName ().Version != expectedVersion)
			Environment.Exit (4);
	}
}
