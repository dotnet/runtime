using System;
using System.Security.Policy;
using System.Runtime.Remoting;
using System.Threading;

class Container {

	static int Main ()
	{
		Console.WriteLine ("Friendly name: " + AppDomain.CurrentDomain.FriendlyName);
			
		AppDomain newDomain = AppDomain.CreateDomain ("NewDomain");

		AppDomain.Unload (newDomain);

		Console.WriteLine("test-ok");

		return 0;
	}
}
