using System;
using System.Security.Policy;
using System.Threading;

class Container {

	[LoaderOptimization (LoaderOptimization.SingleDomain)]
	static void Main ()
	{
		AppDomainSetup setup = new AppDomainSetup ();
		setup.ApplicationBase = ".";

		Console.WriteLine (AppDomain.CurrentDomain.FriendlyName);
			
		AppDomain newDomain = AppDomain.CreateDomain ("NewDomain", new Evidence (), setup);

		string[] args = { "test0", "test1" };
		
		newDomain.ExecuteAssembly ("jit-int.exe");

		Console.WriteLine ("Ready");
	}
}
