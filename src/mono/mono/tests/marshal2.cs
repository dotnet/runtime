using System;
using System.Runtime.InteropServices;

public class Test {


	[StructLayout (LayoutKind.Sequential)]
	public struct SimpleStruct2 {
		public int a;
		public int b;
	}
	
	[StructLayout (LayoutKind.Sequential, CharSet=CharSet.Ansi)]
	public struct SimpleStruct {
		public int a;
		public bool bool1;
		public bool bool2;
		public int b;
		[MarshalAs (UnmanagedType.ByValArray, SizeConst=2)] public short[] a1;
		[MarshalAs (UnmanagedType.ByValTStr, SizeConst=4)] public string s1;
		public SimpleStruct2 emb1;
	}
	
	public unsafe static int Main () {
		SimpleStruct ss = new SimpleStruct ();
		SimpleStruct cp = new SimpleStruct ();
		int size = Marshal.SizeOf (typeof (SimpleStruct));
		
		Console.WriteLine ("SimpleStruct:" + size);
		if (size != 32)
			return 1;
		
		IntPtr p = Marshal.AllocHGlobal (size);
		ss.a = 1;
		ss.bool1 = true;
		ss.bool2 = false;
		ss.b = 2;
		ss.a1 = new short [2];
		ss.a1 [0] = 6;
		ss.a1 [1] = 5;
		ss.s1 = "abcd";
		ss.emb1 = new SimpleStruct2 ();
		ss.emb1.a = 3;
		ss.emb1.b = 4;
				
		Marshal.StructureToPtr (ss, p, false);
		if (Marshal.ReadInt32 (p, 0) != 1)
			return 1;
		if (Marshal.ReadInt32 (p, 4) != 1)
			return 1;
		if (Marshal.ReadInt32 (p, 8) != 0)
			return 1;
		if (Marshal.ReadInt32 (p, 12) != 2)
			return 1;
		if (Marshal.ReadInt16 (p, 16) != 6)
			return 1;
		if (Marshal.ReadInt16 (p, 18) != 5)
			return 1;
		if (Marshal.ReadByte (p, 20) != 97)
			return 1;
		if (Marshal.ReadByte (p, 21) != 98)
			return 1;
		if (Marshal.ReadByte (p, 22) != 99)
			return 1;
		if (Marshal.ReadByte (p, 23) != 0)
			return 1;
		if (Marshal.ReadInt32 (p, 24) != 3)
			return 1;
		if (Marshal.ReadInt32 (p, 28) != 4)
			return 1;

		object o = cp;
		Marshal.PtrToStructure (p, o);
		cp = (SimpleStruct)o;
		
		return 0;
	}
}

