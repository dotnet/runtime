using System;
using System.Collections.Generic;
using System.Reflection;


class Driver {
	/*Test that GC handles interning failure correctly*/
	static void DumpStuff () {
		Console.WriteLine ("CWL under OOM - should not print {0}", 99);
		Console.WriteLine ("CWL under OOM - should not print {0}{1}", 22, 44.4);
	}

	static int Main () {
		Console.WriteLine ("start");
		Assembly corlib = typeof (object).Assembly;
		Module module = corlib.GetModules ()[0];
		var r = new Random (123456);
		var l = new List<object> ();
		try {
			for (int i = 0; i < 400000; ++i) {
				var foo = new byte[r.Next () % 4000];
				l.Add (foo);
			}
			Console.WriteLine ("done");
			return 1;
		} catch (Exception) {
			try {
				DumpStuff ();
				return 2;
			} catch (Exception) {}

			try {
				module.GetTypes ();
				return 3;
			} catch (Exception) {}

			try {
				corlib.GetTypes ();
				return 4;
			} catch (Exception) {}
	

			l.Clear ();
			l = null;
			Console.WriteLine ("OOM done");
		}
		return 0;
	}
}

