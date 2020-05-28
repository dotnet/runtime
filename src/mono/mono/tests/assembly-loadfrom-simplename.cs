using System;
using System.IO;
using System.Reflection;

public class TestAssemblyLoad {

	public static int Main ()
	{
		return TestDriver.RunTests (typeof (TestAssemblyLoad));
	}

	public static bool AnyLoadedAssemblyFrom (string partialPath) {
		bool result = false;
		foreach (var asm in AppDomain.CurrentDomain.GetAssemblies ()) {
			var p = asm.Location;
			if (p != null && p.Contains (partialPath)) {
				Console.Error.WriteLine ("Assembly {0} was unexpectedly loaded from '{1}'", asm.FullName, p);
				result = true;
			}
		}
		return result;
	}

	public static int test_0_LoadFromSimpleNamePreload ()
	{
		
		// The Makefile arranges for assembly-dep-simplename.dll to reference "LibSimpleName, Version=1.0.0.0"
		// At runtime, we will preload "libsimplename, Version=2.0.0.0" (note case and version are different).
		// When we create an instance from assembly-dep-simplename, we expect it to bind to the preloaded libsimplename Version=2.0.0.0, and for no additional assemblies to be loaded.
		string path1 = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "assembly-dep-simplename.dll");
		string path2 = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "assembly-load-dir2", "libsimplename.dll");

		Assembly asm1 = Assembly.LoadFrom (path1);
		if (asm1 == null) {
			Console.Error.WriteLine ("expected asm1 {0} to not be null", asm1);
			return 1;
		}

		Assembly asm2 = Assembly.LoadFrom (path2);
		if (asm2 == null) {
			Console.Error.WriteLine ("expected asm2 {1} to not be null", asm2);
			return 2;
		}
		
		Type t1 = asm1.GetType ("MidClass");
		if (t1 == null) {
			Console.Error.WriteLine ("expected t1 {0} to not be null", t1);
			return 3;
		}

		// causes the reference to libsimplename to be resolved
		var o = Activator.CreateInstance (t1);
			
		FieldInfo f1 = t1.GetField ("X");
		if (f1 == null) {
			Console.Error.WriteLine ("expected to get field MidClass.X, but got {0}", f1);
			return 4;
		}

		int n = (int)f1.GetValue (o);
		if (n != 2) {
			Console.Error.WriteLine ("expected to get the value 2 from MidClass.X, but got {0}", n);
			return 5;
		}

		if (AnyLoadedAssemblyFrom ("assembly-load-dir1")) {
			Console.Error.WriteLine ("An unexpected load event happened (see above)");
			return 6;
		}
		
		return 0;
	}
}
