using System;
using System.IO;
using System.Reflection;

public class TestAssemblyLoad {

	public static int Main ()
	{
		return TestDriver.RunTests (typeof (TestAssemblyLoad));
	}

	public static int test_0_ReflectionOnlyLoadBytesBindingRedirect ()
	{

		string path1 = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "assembly-load-dir1", "LibStrongName.dll");

		// Try to load from dir1/LibStrongName.dll, despite
		// the assembly-refonly-load-bytes-bindingredirect.exe.config redirecting old versions to
		// be mapped to version 2.0.0.0 which is in the assembly-load-dir2 directory
		//
		// The binding redict should not apply to reflection-only loads.
		byte[] bytes1 = File.ReadAllBytes (path1);
		Assembly asm1 = Assembly.ReflectionOnlyLoad (bytes1);
		if (asm1 == null) {
			Console.Error.WriteLine ("expected asm1 {0} to not be null", asm1);
			return 1;
		}

		Type t1 = asm1.GetType ("LibClass");
		if (t1 == null) {
			Console.Error.WriteLine ("expected t1 {0} to not be null", t1);
			return 2;
		}

		FieldInfo f1 = t1.GetField ("OnlyInVersion1");
		if (f1 == null) {
			Console.Error.WriteLine ("expected to find field OnlyInVersion1, but got null");
			return 3;
		}

		if (f1.FieldType != typeof(int)) {
			Console.Error.WriteLine ("Field OnlyInVersion1 has type {0}, expected int", f1.FieldType);
			return 5;
		}

		FieldInfo f2 = t1.GetField ("OnlyInVersion2");

		if (f2 != null) {
			Console.Error.WriteLine ("expected not to find field OnlyInVersion2, but got {0}", f2);
			return 4;
		}

		return 0;
	}
}
