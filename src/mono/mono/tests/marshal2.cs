//
// Tests for Marshal.StructureToPtr and PtrToStructure
//

using System;
using System.Text;
using System.Runtime.InteropServices;

public class Tests {


	[StructLayout (LayoutKind.Sequential)]
	public class SimpleObj {
		public int a;
		public int b;

		public void test () {}
	}

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
		public SimpleObj emb2;
		public string s2;
		public double x;
		[MarshalAs (UnmanagedType.ByValArray, SizeConst=2)] public char[] a2;
	}

	[StructLayout (LayoutKind.Sequential, CharSet=CharSet.Ansi)]
	public struct ByValTStrStruct {
		[MarshalAs (UnmanagedType.ByValTStr, SizeConst=4)] public string s1;
		public int i;
	}

	[StructLayout (LayoutKind.Sequential, CharSet=CharSet.Unicode)]
	public struct ByValWStrStruct {
		[MarshalAs (UnmanagedType.ByValTStr, SizeConst=4)] public string s1;
		public int i;
	}

	[StructLayout (LayoutKind.Sequential, Pack=1)]
	public struct PackStruct1 {
		float f;
	}

	[StructLayout (LayoutKind.Sequential)]
	public struct PackStruct2 {
		byte b;
		PackStruct1 s;
	}
	
	public unsafe static int Main (String[] args) {
		if (TestDriver.RunTests (typeof (Tests), args) != 0)
			return 34;
		return 0;
	}

	public static int test_0_structure_to_ptr () {
		SimpleStruct ss = new SimpleStruct ();
		int size = Marshal.SizeOf (typeof (SimpleStruct));
		
		//if (size != 52)
		//return 1;
		
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
		ss.emb2 = new SimpleObj ();
		ss.emb2.a = 10;
		ss.emb2.b = 11;
		ss.s2 = "just a test";
		ss.x = 1.5;
		ss.a2 = new char [2];
		ss.a2 [0] = 'a';
		ss.a2 [1] = 'b';
		
		Marshal.StructureToPtr (ss, p, false);
		Type t = ss.GetType ();
		
		if (Marshal.ReadInt32 (p, (int)Marshal.OffsetOf (t, "a")) != 1)
			return 1;
		if (Marshal.ReadInt32 (p, (int)Marshal.OffsetOf (t, "bool1")) != 1)
			return 2;
		if (Marshal.ReadInt32 (p, (int)Marshal.OffsetOf (t, "bool2")) != 0)
			return 3;
		if (Marshal.ReadInt32 (p, (int)Marshal.OffsetOf (t, "b")) != 2)
			return 4;
		if (Marshal.ReadInt16 (p, 16) != 6)
			return 5;
		if (Marshal.ReadInt16 (p, 18) != 5)
			return 6;
		if (Marshal.ReadByte (p, 20) != 97)
			return 7;
		if (Marshal.ReadByte (p, 21) != 98)
			return 8;
		if (Marshal.ReadByte (p, 22) != 99)
			return 9;
		if (Marshal.ReadByte (p, 23) != 0)
			return 10;
		if (Marshal.ReadInt32 (p, 24) != 3)
			return 11;
		if (Marshal.ReadInt32 (p, 28) != 4)
			return 12;
		if (Marshal.ReadInt32 (p, 32) != 10)
			return 13;
		if (Marshal.ReadInt32 (p, 36) != 11)
			return 14;
		if (Marshal.ReadByte (p, (int)Marshal.OffsetOf (t, "a2")) != 97)
			return 15;
		if (Marshal.ReadByte (p, (int)Marshal.OffsetOf (t, "a2") + 1) != 98)
			return 16;

		SimpleStruct cp = (SimpleStruct)Marshal.PtrToStructure (p, ss.GetType ());

		if (cp.a != 1)
			return 16;

		if (cp.bool1 != true)
			return 17;

		if (cp.bool2 != false)
			return 18;

		if (cp.b != 2)
			return 19;

		if (cp.a1 [0] != 6)
			return 20;
		
		if (cp.a1 [1] != 5)
			return 21;

		if (cp.s1 != "abc")
			return 22;
		
		if (cp.emb1.a != 3)
			return 23;

		if (cp.emb1.b != 4)
			return 24;

		if (cp.emb2.a != 10)
			return 25;

		if (cp.emb2.b != 11)
			return 26;

		if (cp.s2 != "just a test")
			return 27;

		if (cp.x != 1.5)
			return 28;

		if (cp.a2 [0] != 'a')
			return 29;

		if (cp.a2 [1] != 'b')
			return 30;
		return 0;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
	public struct Struct1
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        public string Field1;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string Field2;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string Field3;
	}

	public static int test_0_byvaltstr () {
		ByValTStrStruct s = new ByValTStrStruct ();

		IntPtr p2 = Marshal.AllocHGlobal (Marshal.SizeOf (typeof (ByValTStrStruct)));
		Marshal.StructureToPtr(s, p2, false);

		/* Check that the ByValTStr is initialized correctly */
		for (int i = 0; i < 4; ++i)
			if (Marshal.ReadByte (p2, i) != 0)
				return 31;

		s.s1 = "ABCD";
		s.i = 55;

		Marshal.StructureToPtr(s, p2, false);

		ByValTStrStruct s2 = (ByValTStrStruct)Marshal.PtrToStructure (p2, typeof (ByValTStrStruct));

		/* The fourth char is lost because of null-termination */
		if (s2.s1 != "ABC")
			return 32;

		if (s2.i != 55)
			return 33;

		// Check that decoding also respects the size, even when there is no null terminator
		byte[] data = Encoding.ASCII.GetBytes ("ABCDXXXX");
		int size = Marshal.SizeOf (typeof (ByValTStrStruct));
		IntPtr buffer = Marshal.AllocHGlobal (size);
		Marshal.Copy (data, 0, buffer, size);

		s2 = (ByValTStrStruct)Marshal.PtrToStructure (buffer, typeof (ByValTStrStruct));
		if (s2.s1 != "ABC")
			return 34;

		return 0;
	}

	public static int test_0_byvaltstr_unicode () {
		ByValWStrStruct s = new ByValWStrStruct ();

		IntPtr p2 = Marshal.AllocHGlobal (Marshal.SizeOf (typeof (ByValWStrStruct)));
		Marshal.StructureToPtr(s, p2, false);

		/* Check that the ByValWStr is initialized correctly */
		for (int i = 0; i < 8; ++i)
			if (Marshal.ReadByte (p2, i) != 0)
				return 31;

		s.s1 = "ABCD";
		s.i = 55;

		Marshal.StructureToPtr(s, p2, false);

		ByValWStrStruct s2 = (ByValWStrStruct)Marshal.PtrToStructure (p2, typeof (ByValWStrStruct));

		/* The fourth char is lost because of null-termination */
		if (s2.s1 != "ABC")
			return 32;

		if (s2.i != 55)
			return 33;
		return 0;
	}

	public static int test_0_byvaltstr_max_size () {
		string buffer = "12345678123456789012345678901234";

		IntPtr ptr = Marshal.StringToBSTR (buffer);

		Struct1 data = (Struct1)Marshal.PtrToStructure (ptr, typeof (Struct1));
		if (data.Field1 != "12345678")
			return 1;
		if (data.Field2 != "1234567890")
			return 2;
		if (data.Field3 != "12345678901234")
			return 3;
		return 0;
	}

	// Check that the 'Pack' directive on a struct changes the min alignment of the struct as well (#12110)
	public static int test_0_struct_pack () {
		if (Marshal.OffsetOf (typeof (PackStruct2), "s") != new IntPtr (1))
			return 1;
		return 0;
	}

	public static int test_0_generic_ptr_to_struct () {
		int size = Marshal.SizeOf (typeof (SimpleStruct2));
		IntPtr p = Marshal.AllocHGlobal (size);

		Marshal.WriteInt32 (p, 0, 1); //a
		Marshal.WriteInt32 (p, 4, 2); //a

		var s = Marshal.PtrToStructure<SimpleStruct2> (p);

		if (s.a != 1)
			return 1;
		if (s.b != 2)
			return 2;
		return 0;
	}
}
