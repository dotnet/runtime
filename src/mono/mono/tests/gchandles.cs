using System;
using System.Threading;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
internal struct ObjectWrapper {
	[FieldOffset(0)] object obj;
	[FieldOffset(0)] IntPtr handle;

	internal static IntPtr Convert (object obj) {
		ObjectWrapper wrapper = new ObjectWrapper ();

		wrapper.obj = obj;

		return wrapper.handle;
	}
	
	internal static object Convert (IntPtr ptr) 
	{
		ObjectWrapper wrapper = new ObjectWrapper ();
		
		wrapper.handle = ptr;
			
		return wrapper.obj;
	}
}

public class Test1
{
	public GCHandle self;
	public static bool fail;
	public static Test1 instance;

	~Test1 () {
		if (self.Target == null)
			fail = true;
	}
}

public class Test2
{
	public GCHandle self;
	public static bool fail;
	public static Test2 instance;

	~Test2 () {
		if (self.Target == null)
			fail = true;
	}
}

public class Tests
{
	public static int Main (String[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}

	static void create1 () {
		Test1.instance = new Test1 ();
		Test1.instance.self = GCHandle.Alloc (Test1.instance, GCHandleType.WeakTrackResurrection);
		Test1.instance = null;
	}

	public static unsafe int test_0_track_resurrection () {
		/* The GCHandle should not be cleared before calling the finalizers */
		create1 ();
		GC.Collect ();
		GC.WaitForPendingFinalizers ();

		// WaitForPendingFinalizers doesn't seem to work ?
		System.Threading.Thread.Sleep (100);
		/* If the finalizers do not get ran by this time, the test will still succeed */
		return Test1.fail ? 1 : 0;
	}

	static void create2 () {
		Test2.instance = new Test2 ();
		object o = new object ();
		Test2.instance.self = GCHandle.Alloc (o, GCHandleType.WeakTrackResurrection);
		Test2.instance.self.Target = Test2.instance;
		Test2.instance = null;
	}

	public static int test_0_track_resurrection_set_target () {
		/* Same test but the handle target is set dynamically after it is created */
		create2 ();
		GC.Collect ();
		GC.WaitForPendingFinalizers ();

		System.Threading.Thread.Sleep (100);
		return Test2.fail ? 1 : 0;
	}

	static GCHandle handle;
	static IntPtr original_addr;
	
	static void alloc_handle () {
		var obj = new object ();
		handle = GCHandle.Alloc (obj, GCHandleType.Pinned);
		original_addr =  ObjectWrapper.Convert (obj);
	}

	public static int test_0_pinned_handle_pins_target () {
		var t = new Thread (alloc_handle);
		t.Start ();
		t.Join ();
		GC.Collect ();

		var newAddr = ObjectWrapper.Convert (handle.Target);

		return original_addr == newAddr ? 0 : 1;
	}
}