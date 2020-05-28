using System;
using System.IO;
using System.Reflection;

public class TestAssemblyLoad {

	public static int Main ()
	{
		return TestDriver.RunTests (typeof (TestAssemblyLoad));
	}

	public static int test_0_LoadFromBindingRedirect ()
	{
		
		string path1 = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "assembly-load-dir1", "LibStrongName.dll");

		// Try to load from dir1/LibStrongName.dll, but
		// the assembly-loadfrom-bindingredirect.exe.config redirects all old versions to
		// be mapped to version 2.0.0.0 which is in the assembly-load-dir2 directory
		//
		// This is a regression test for https://github.com/mono/mono/issues/8152
		Assembly asm1 = Assembly.LoadFrom (path1);
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
		if (f1 != null) {
			Console.Error.WriteLine ("expected not to find field OnlyInVersion1, but got {0}", f1);
			return 3;
		}

		FieldInfo f2 = t1.GetField ("OnlyInVersion2");

		if (f2 == null) {
			Console.Error.WriteLine ("expected field OnlyInVersion2 not to be null");
			return 4;
		}

		if (f2.FieldType != typeof(int)) {
			Console.Error.WriteLine ("Field OnlyInVersion2 has type {0}, expected int", f2.FieldType);
			return 5;
		}
		return 0;
	}
}
