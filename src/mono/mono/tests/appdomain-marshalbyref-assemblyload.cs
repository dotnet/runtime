using System;
using System.IO;
using System.Security.Policy;

public class Test {
	public static int Main ()
	{
		var dir1 = Path.GetDirectoryName (typeof (Test).Assembly.Location);
		var dir2 = "appdomain-marshalbyref-assemblyload2";
		var domain = CreateDomain (dir2);
		var path1 = Path.Combine (dir1, "MidAssembly.dll");
		object o = domain.CreateInstanceFromAndUnwrap (path1, "MidAssembly.MidClass");
		var mc = o as MidAssembly.MidClass;
		var l = new LeafAssembly.Leaf ();
		/* passing Leaf to MidMethod should /not/ cause
		 * place2/LeafAssembly.dll to be loaded in the new remote
		 * domain */
		mc.MidMethod (l);
		/* this line will pre-load place1/LeafAssembly.dll into the
		 * remote domain.
		 */
		mc.ForceLoadFrom (Path.Combine (dir1, "LeafAssembly.dll"));
		/* This method calls a class from LeafAssembly (which is only
		 * defined in the place1 version of the class), so if the
		 * place2 version had been loaded instead, it will trigger a
		 * MissingMethodException */
		mc.DoSomeAction ();
		return 0;
	}

	public static AppDomain CreateDomain (string newpath)
	{
		var appDomainSetup = new AppDomainSetup ();
		appDomainSetup.ApplicationBase = newpath;
		var appDomain = AppDomain.CreateDomain ("MyDomainName", new Evidence (), appDomainSetup);
		return appDomain;
	}

}
