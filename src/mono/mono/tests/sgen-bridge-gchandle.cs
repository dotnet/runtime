using System;
using System.Collections;
using System.Threading;
using System.Runtime.InteropServices;


public class Bridge {
	public int __test;
	public string id;
	
	~Bridge () {
		try {Console.WriteLine ("bridge {0} gone", id);} catch (Exception) {}
	}
}


/*
Test scenario:
	Alloc a bridge and create a gc handle to it
	Get it collected.
	Create another one and see it steal the handle of the previous one.


*/
class Driver {
	public static GCHandle weak_track_handle;
	public static GCHandle weak_track_handle2;

	static void CreateFirstBridge () {
		Bridge b = new Bridge() {
			__test = 0,
			id = "first",
		};
		weak_track_handle = GCHandle.Alloc (b, GCHandleType.WeakTrackResurrection);
	}

	static void CreateSecondBridge () {
		Bridge b = new Bridge() {
			__test = 1,
			id = "second",
		};
		weak_track_handle2 = GCHandle.Alloc (b, GCHandleType.WeakTrackResurrection);
	}

	static void DumpHandle (GCHandle h, string name) {
		Console.WriteLine ("{0}:{1:X} alloc:{2} hasValue:{2}", name, (IntPtr)h, h.IsAllocated, h.Target == null);
	}

	static int Main () {
		var t = new Thread (CreateFirstBridge);
		t.Start ();
		t.Join ();

		GC.Collect ();
		GC.WaitForPendingFinalizers ();
		Console.WriteLine ("GC DONE");

		DumpHandle (weak_track_handle, "weak-track1");

		t = new Thread (CreateSecondBridge);
		t.Start ();
		t.Join ();

		GC.Collect ();
		GC.WaitForPendingFinalizers ();
		Console.WriteLine ("GC DONE");
		DumpHandle (weak_track_handle, "weak-track1");
		DumpHandle (weak_track_handle2, "weak-track2");
		Console.WriteLine ("DONE");

		if ((IntPtr)weak_track_handle == (IntPtr)weak_track_handle2) {
			Console.WriteLine ("FIRST HANDLE GOT DEALLOCATED!");
			return 1;
		}

		return 0;
	}
}
