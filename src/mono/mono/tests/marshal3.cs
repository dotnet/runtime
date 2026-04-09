using System;
using System.Runtime.InteropServices;

public class Test {


	[StructLayout (LayoutKind.Sequential)]
	public struct BlittableStruct {
		public int a;
		public int b;
	}
	
	public unsafe static int Main () {
		BlittableStruct ss = new BlittableStruct ();
		int size = Marshal.SizeOf (typeof (BlittableStruct));
		
		Console.WriteLine ("BlittableStruct:" + size);
		if (size != 8)
			return 1;
		
		IntPtr p = Marshal.AllocHGlobal (size);
		ss.a = 123;
		ss.b = 124;

		Marshal.StructureToPtr (ss, p, false);
		Type t = ss.GetType ();
		
		if (Marshal.ReadInt32 (p, 0) != 123)
			return 1;
		if (Marshal.ReadInt32 (p, 4) != 124)
			return 1;

		BlittableStruct cp = (BlittableStruct)Marshal.PtrToStructure (p, ss.GetType ());

		if (cp.a != 123)
			return 2;

		if (cp.b != 124)
			return 2;
		
		return 0;
	}
}

