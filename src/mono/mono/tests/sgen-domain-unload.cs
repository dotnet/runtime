using System;
using System.Collections.Generic;

public class Bar {
	public object a, b, c;
	
}
class Driver {
	static void ProduceSimpleHeapWithLOS () {
		Console.WriteLine ("running in {0}", AppDomain.CurrentDomain);
		byte[] a = new byte [4 * 1000 * 1000];
		byte[] b = new byte [4 * 1000 * 1000];
		byte[] c = new byte [4 * 1000 * 1000];
		var lst = new List<object> ();

		Bar la, lb, lc;
		la = lb = lc = null;
		for (int i = 0; i < 1000 * 200; ++i) {
			var ba = new Bar ();
			var bb = new Bar ();
			var bc = new Bar ();
			ba.a = la;
			ba.b = bb;
			ba.c = a;

			bb.a = bc;
			ba.b = b;
			bb.c = lb;
			
			bc.a = c;
			bc.b = lc;
			bc.c = ba;

			la = ba;
			lb = bb;
			lc = bc;

			lst.Add (ba);
		}
		
	}

	static void SimpleHeapWithLOS () {
		ProduceSimpleHeapWithLOS ();
	}

	static void CrossDomainTest (string name, CrossAppDomainDelegate dele) {
		TestTimeout timeout = TestTimeout.Start (TimeSpan.FromSeconds(TestTimeout.IsStressTest ? 60 : 5));
		Console.WriteLine ("----Testing {0}----", name);
		for (int i = 0; timeout.HaveTimeLeft; ++i) {
			var ad = AppDomain.CreateDomain (string.Format ("domain-{0}-{1}", name, i));
			ad.DoCallBack (dele);
			AppDomain.Unload (ad);
		}
	}

	static void Main () {
		CrossDomainTest ("simple-heap-with-los", Driver.SimpleHeapWithLOS);
	}
}
