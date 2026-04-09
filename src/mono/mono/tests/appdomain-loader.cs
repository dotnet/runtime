using System;

class Driver {
	static void Main () {
		for (int i = 0; i < 10; ++i) {
			var ad = AppDomain.CreateDomain ("ad3");
			ad.ExecuteAssembly ("appdomain-tester.exe", null, null);
			AppDomain.Unload (ad);
		}
	}
}