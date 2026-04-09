using System;
using System.IO;
using System.Reflection;

public class TestAssemblyLoad {

	public static int Main ()
	{
		return TestDriver.RunTests (typeof (TestAssemblyLoad));
	}

	public static int test_0_LoadFromSameAssemblyName ()
	{
		
		string path1 = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "assembly-load-dir1", "Lib.dll");
		string path2 = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "assembly-load-dir2", "Lib.dll");

		Assembly asm1 = Assembly.LoadFrom (path1);
		Assembly asm2 = Assembly.LoadFrom (path2);
		if (asm1 != asm2) {
			Console.Error.WriteLine ("expected asm1 {0} and asm2 {1} to be the same", asm1, asm2);
			return 1;
		}

		Type t1 = asm1.GetType ("LibClass");
		Type t2 = asm2.GetType ("LibClass");
		if (t1 != t2) {
			Console.Error.WriteLine ("expected t1 {0} and t2 {1} to be the same", t1, t2);
			return 2;
		}
			
		object o1 = Activator.CreateInstance (t1);
		object o2 = Activator.CreateInstance (t2);

		string s1 = o1.ToString ();
		string s2 = o2.ToString ();

		if (s1 != s2) {
			Console.Error.WriteLine ("expected s1 {0} and s1 {1} to be the same", s1, s2);
			return 3;
		}

		return 0;
	}
}
