using System;
using System.IO;
using System.Reflection;

class Program
{
	static int Main ()
	{
		string assemblyFile = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "bug-81691-b.dll");
		Assembly assembly = Assembly.LoadFile (assemblyFile);
		int i;
		int numExceptions = 0;

		for (i = 0; i < 100000; ++i) {
			try {
				try {
					Type type = assembly.GetType ("NS.B.TestB");
					FieldInfo field =type.GetField ("testb", BindingFlags.NonPublic | BindingFlags.Static);
					if (field.FieldType == null)
						return 1;
				} catch (TypeLoadException ex) {
					++numExceptions;
				}
			} catch (FileNotFoundException ex) {
				++numExceptions;
			}
		}

		if (numExceptions == 100000)
			return 0;
		return 1;
	}
}
