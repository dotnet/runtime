using System;
using System.Threading;

/*
This test stresses the interaction of the multiple suspend sources and stop-the-world.

Right now the current iteraction that we stresses is between the domain unloader and
sgen STW. It's mighty hard to get this right on mach.
*/

class Driver {

	static void AllocStuff ()
	{
		var x = new object ();
		for (int i = 0; i < 300; ++i)
			x = new byte [i];
	}

	static void BackgroundNoise ()
	{
		int i = 0;
		while (true) {
			AllocStuff ();
			++i;
		}
	}

	static void AppDomainBackgroundNoise ()
	{
		for (int i = 0; i < 3; ++i) {
			var t = new Thread (BackgroundNoise);
			t.IsBackground = true;
			t.Start ();
		}
	}

	static void Main () {
		for (int i = 0; i < 3; ++i) {
			var t = new Thread (BackgroundNoise);
			t.IsBackground = true;
			t.Start ();
		}
		
		for (int i = 0; i < 100; ++i) {
			var ad = AppDomain.CreateDomain ("domain_" + i);
			ad.DoCallBack (new CrossAppDomainDelegate (AppDomainBackgroundNoise));
			Thread.Sleep (10);
			AppDomain.Unload (ad);
			Console.Write (".");
			if (i > 0 && i % 20 == 0) Console.WriteLine ();
		}
		Console.WriteLine ("\ndone");
	}
}