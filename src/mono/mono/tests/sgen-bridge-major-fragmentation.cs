using System;
using System.Threading;

//64 / 128 size
public class NonBridge {
	public object a,b,c,d,e,f,g,h,i,j,k,l,m,n;
}

public class Bridge {
	public object a,b,c,d,e,f,g,h,i,j,k,l,m,n;
	
	~Bridge () {}
}

class Driver {
	//we fill 16Mb worth of stuff, eg, 256k objects
	const int major_fill = 1024 * 256;

	//4mb nursery with 64 bytes objects -> alloc half
	const int nursery_obj_count = 16 * 1024;

	static void CrashMainLoop () {
		var arr = new object [major_fill];
		for (int i = 0; i < major_fill; ++i)
			arr [i] = new NonBridge ();
		GC.Collect (1);
		Console.WriteLine ("major fill done");

		//induce massive fragmentation
		for (int i = 0; i < major_fill; i += 4) {
			arr [i + 1] = null;
			arr [i + 2] = null;
			arr [i + 3] = null;
		}
		GC.Collect (1);
		Console.WriteLine ("fragmentation done");

		//since 50% is garbage, do 2 fill passes
		for (int j = 0; j < 2; ++j) {
			for (int i = 0; i < major_fill; ++i) {
				if ((i % 1000) == 0)
					new Bridge ();
				else
					arr [i] = new Bridge ();
			}
		}
		Console.WriteLine ("done spewing bridges");
		
		for (int i = 0; i < major_fill; ++i)
			arr [i] = null;
		GC.Collect ();
	}
	

	static void Main () {
		const int loops = 5;
		for (int i = 0; i < loops; ++i) {
			Console.WriteLine ("CrashLoop {0}/{1}", i + 1, loops);
			CrashMainLoop ();
		}
		Console.WriteLine ("done");
		GC.Collect ();
		GC.WaitForPendingFinalizers ();
	}
}
