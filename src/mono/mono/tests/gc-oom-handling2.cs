using System;
using System.Collections.Generic;

class Driver {
	static int Main () {
		Console.WriteLine ("start");
		var r = new Random (123456);
		var l = new List<object> ();
		try {
			for (int i = 0; i < 40000; ++i) {
				var foo = new byte[r.Next () % 4000];
				l.Add (foo);
			}
			Console.WriteLine ("done");
			return -1;
		} catch (Exception e) {
			l.Clear ();
			l = null;
			Console.WriteLine ("OOM done");
		}
		return 0;
	}
}

