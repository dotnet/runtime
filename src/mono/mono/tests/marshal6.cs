using System;
using System.Runtime.InteropServices;


[StructLayout(LayoutKind.Explicit, Size=32)]
public class Dummy {
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst=16)]
	[FieldOffset(0)]
	public string	a;
}

public class X {
	public static int Main () {
		Dummy dummy = new Dummy ();
		dummy.a = "abcd";

		IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Dummy)));
		Marshal.StructureToPtr(dummy, p, false);
		
		if(Marshal.ReadInt32(p, 0) != 0x64636261) {
			return 1;
		}
		return 0;
	}
}
