using System;
using System.Security.Policy;
using System.Threading;

class Container {

	static int Main ()
	{
		AppDomainSetup setup = new AppDomainSetup ();
		setup.ApplicationBase = ".";

		Console.WriteLine (AppDomain.CurrentDomain.FriendlyName);
			
		AppDomain newDomain = AppDomain.CreateDomain ("NewDomain", new Evidence (), setup);

		newDomain.SetData ("TEST", "a");
		if ((string)newDomain.GetData ("TEST") != "a")
			return 1;

		newDomain.SetData ("TEST", 1);
		if ((int)newDomain.GetData ("TEST") != 1)
			return 1;

		newDomain.SetData ("TEST", true);
		if ((bool)newDomain.GetData ("TEST") != true)
			return 1;

		newDomain.SetData ("TEST", false);
		if ((bool)newDomain.GetData ("TEST") != false)
			return 1;

		int [] ta = { 1, 2, 3 };
		newDomain.SetData ("TEST", ta);

		int [] ca = (int [])newDomain.GetData ("TEST");
		
		if (ca [0] != 1 || ca [1] != 2 || ca [2] != 3)
			return 1;
		
		return 0;
	}
}
