using System;
using System.Collections.Generic;

class Driver {
	static int Main () {
		Console.WriteLine ("start");
		var l = new List<object> ();
		try {
			for (int i = 0; i < 40000; ++i) {
				var foo = new byte[2000];
				//Console.WriteLine ("done {0}",i);
				if (foo == null)
					Console.WriteLine ("WTF");
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

