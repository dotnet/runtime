using System;
using System.Reflection;
using System.Reflection.Emit;


public class TestAssemblyResolveEvent {
	public static int Main (String[] args) {
		// Regression test for https://bugzilla.xamarin.com/show_bug.cgi?id=57851

		// If the custom attributes of an assembly trigger a
		// ResolveEventHandler, and that handler returns an
		// AssemblyBuilder, don't crash.
		var h = new MockResolver ("assemblyresolve_event5_label");
		var aName = new AssemblyName ("assemblyresolve_event5_helper");
		var a = AppDomain.CurrentDomain.Load (aName);
		var t = a.GetType ("MyClass");
		h.StartHandling ();
		var cas = t.GetCustomAttributes (true);
		h.StopHandling ();
		return 0;
	}
}


public class MockResolver {
	private Assembly mock;
	private ResolveEventHandler d;
	private string theName;
	
	public MockResolver (string theName) {
		mock = CreateMock (theName);
		d = new ResolveEventHandler (HandleResolveEvent);
		this.theName = theName;
	}

	public void StartHandling () {
		AppDomain.CurrentDomain.AssemblyResolve += d;
	}

	public void StopHandling () {
		AppDomain.CurrentDomain.AssemblyResolve -= d;
	}

	public Assembly HandleResolveEvent (Object sender, ResolveEventArgs args) {
		Console.Error.WriteLine ("handling load of {0}", args.Name);
		if (args.Name.StartsWith (theName))
			return mock;
		else
			return null;
	}

	private static Assembly CreateMock (string s) {
		var an = new AssemblyName (s);
		var ab = AssemblyBuilder.DefineDynamicAssembly (an, AssemblyBuilderAccess.Run);
		var mb = ab.DefineDynamicModule (an.Name);

		var tb = mb.DefineType ("Foo", TypeAttributes.Public);
		tb.DefineDefaultConstructor (MethodAttributes.Public);
		tb.CreateType ();

		return ab;
	}
}
