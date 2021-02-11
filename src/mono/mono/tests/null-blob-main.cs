using System;
using System.Reflection;

public class M {
	public static int Main ()
	{
		var an = new AssemblyName ("null-blob-ref");
		var assm = Assembly.Load (an);
		var t = assm.GetType ("C2");
		var o = Activator.CreateInstance (t);
		var mi = t.GetMethod ("M1");
		// This method call to C2.M1 causes a load of null-blob-ref.dll
		// which will forward to null-blob-null-blob-assm.dll which is
		// an assembly with a size zero Blob stream. Loading
		// null-blob-null-blob-assm.dll should not trigger a crash
		mi.Invoke (o, new object[] { null } );
		return 0;
	}
}
