using System;
using System.Collections.Generic;

class Driver {
	static int Main () {
		Console.WriteLine ("start");
		var l = new object[40000];
		try {
			for (int i = 0; i < 40000; ++i) {
				var foo = new byte[2000];
				l[i] = foo;
			}
			Console.WriteLine ("done");
			return 1;
		} catch (Exception e) {
			/*Create massive fragmentation - hopefully*/
			for (int i = 0; i < 40000; i += 2)
				l[i] = null;
			/*Fist major schedule the given block range for evacuation*/
			GC.Collect ();
			/*Second major triggers evacuation*/
			GC.Collect ();
			Array.Clear (l, 0, 40000);
			l = null;
			Console.WriteLine ("OOM done");
		}
		return 0;
	}
}

