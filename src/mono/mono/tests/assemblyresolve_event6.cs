using System;
using System.IO;
using System.Threading;
using System.Reflection;

public class App
{
	public static int Main ()
	{
		/* Regression test for #58950: When the
		 * ReflectionOnlyAssemblyResolve event handler throws an
		 * exception, mono would unwind native code in the loader,
		 * which left stale coop handles on the coop handle stack.
		 * Then, the domain unload, asserted in
		 * mono_handle_stack_free_domain (). */
		var d = AppDomain.CreateDomain ("TestDomain");
		var o = d.CreateInstanceAndUnwrap (typeof (App).Assembly.FullName, "App/Work") as Work;
		var r = o.DoSomething ();
		if (r != 0)
			return r;
		AppDomain.Unload (d);
		return 0;
	}

	public class MyExn : Exception {
		public MyExn () : base ("MyReflectionResolveEventHandler threw") {}
	}

	public class Work : MarshalByRefObject {
		public Work () { }

		public int DoSomething ()
		{
			AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += new ResolveEventHandler (MyReflectionResolveEventHandler);
			bool caught = false;
			try {
				Assembly a = Assembly.ReflectionOnlyLoadFrom ("assemblyresolve_asm.dll");
				var t = a.GetType ("Asm2");
				var m = t.GetMethod ("M"); // This triggers a load of TestBase
				Console.Error.WriteLine ("got '{0}'", m);
			} catch (FileNotFoundException e) {
				Console.WriteLine ("caught FNFE {0}", e);
				caught = true;
			} catch (MyExn ) {
				Console.Error.WriteLine ("caught MyExn, should've been a FNFE");
				return 2;
			}
			if (!caught) {
				Console.Error.WriteLine ("expected to catch a FNFE");
				return 3;
			}
			return 0;
		}

		static Assembly MyReflectionResolveEventHandler (object sender, ResolveEventArgs args) {
			Console.Error.WriteLine ($"Load event for: {args.Name}");
			if (args.Name == "Test, Version=0.0.0.0, Culture=neutral" || args.Name == "Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
				return Assembly.ReflectionOnlyLoadFrom (Path.Combine (Directory.GetCurrentDirectory (), "assemblyresolve_deps", "Test.dll"));
			// a request to load TestBase will throw here, which
			// should be caught in the runtime
			throw new MyExn ();
		}
	}
}
