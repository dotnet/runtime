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
		
		if(Marshal.ReadByte(p, 0) != 0x61) {
			return 1;
		}
		if(Marshal.ReadByte(p, 1) != 0x62) {
			return 1;
		}
		if(Marshal.ReadByte(p, 2) != 0x63) {
			return 1;
		}
		if(Marshal.ReadByte(p, 3) != 0x64) {
			return 1;
		}
		return 0;
	}
}
