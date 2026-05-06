using System;
using System.Collections;
using System.Threading;

public class Bridge {
	public static int bridges_done;
	
	public object[] links = new object [10];
	~Bridge () {
		++bridges_done;
	}
}

class Driver {
	static int Main () {
		int count = Environment.ProcessorCount + 2;
		var th = new Thread [count]; 
		for (int i = 0; i < count; ++i) {
			th [i] = new Thread ( _ =>
			{
				var lst = new ArrayList ();
				for (var j = 0; j < 5 * 1000 * 1000; j++) {
					lst.Add (new object ());
					if ((j % 9999) == 0)
						lst.Add (new Bridge ());
					if ((j % 10000) == 0)
						new Bridge ();
					if ((j % 500000) == 0)
						lst = new ArrayList ();
					
				}
		    });
		
			th [i].Start ();
		}

		for (int i = 0; i < count; ++i)
			th [i].Join ();

		GC.Collect (2);
		
		return Bridge.bridges_done > 0 ? 0 : 1;
	}
}