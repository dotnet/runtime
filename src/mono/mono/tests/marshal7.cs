using System;
using System.Reflection;
using System.Runtime.InteropServices;

public class Test 
{
	[StructLayout(LayoutKind.Sequential, Size=32)]
	public class TestStruct1 
	{
		public int a;
	}

	[StructLayout(LayoutKind.Sequential, Size=32)]
	public class TestStruct2 
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=16)]
		public string	a;
		public int		b;
	}

	[StructLayout(LayoutKind.Sequential, Size=32)]
	public class TestStruct3 : TestStruct2
	{
		public int		c;
	}

	[StructLayout (LayoutKind.Sequential)]
	public class TestStruct4
	{
		[MarshalAs (UnmanagedType.Interface)]
		object itf;
	}

	[StructLayout (LayoutKind.Sequential)]
	public class TestStruct5
	{
		[MarshalAs (UnmanagedType.IUnknown)]
		object itf;
	}

	[StructLayout (LayoutKind.Sequential)]
	public class TestStruct6
	{
		[MarshalAs (UnmanagedType.IDispatch)]
		object itf;
	}

	[StructLayout (LayoutKind.Sequential)]
	public class TestStruct7
	{
		[MarshalAs (UnmanagedType.Struct)]
		object itf;
	}

	// Size should be 16 in both 32 and 64 bits win/linux
	// Size should be 12 on 32bits OSX size alignment of long is 4
	[StructLayout (LayoutKind.Explicit)]
	struct TestStruct8 {
		[FieldOffset (0)]
		public int a;
		[FieldOffset (4)]
		public ulong b;
	}

	// Size should be 12 in both 32 and 64 bits
	[StructLayout (LayoutKind.Explicit, Size=12)]
	struct TestStruct9 {
		[FieldOffset (0)]
		public int a;
		[FieldOffset (4)]
		public ulong b;
	}

	// Size should be 16 in both 32 and 64 bits
	// Size should be 12 on 32bits OSX size alignment of long is 4
	[StructLayout (LayoutKind.Explicit)]
	struct TestStruct10 {
		[FieldOffset (0)]
		public int a;
		[FieldOffset (3)]
		public ulong b;
	}

	// Size should be 11 in both 32 and 64 bits
	[StructLayout (LayoutKind.Explicit, Size=11)]
	struct TestStruct11 {
		[FieldOffset (0)]
		public int a;
		[FieldOffset (3)]
		public ulong b;
	}

	[StructLayout (LayoutKind.Explicit, Pack=1)]
	struct TestStruct12 {
		[FieldOffset (0)]
		public short a;
		[FieldOffset (2)]
		public int b;
	}

	// Size should always be 12, since pack = 0, size = 0 and min alignment = 4
	//When pack is not set, we default to 8, so min (8, min alignment) -> 4
	[StructLayout (LayoutKind.Explicit)]
	struct TestStruct13 {
		[FieldOffset(0)]
		int one;
		[FieldOffset(4)]
		int two;
		[FieldOffset(8)]
		int three;
	}

	// Size should always be 12, since pack = 8, size = 0 and min alignment = 4
	//It's aligned to min (pack, min alignment) -> 4
	[StructLayout (LayoutKind.Explicit)]
	struct TestStruct14 {
		[FieldOffset(0)]
		int one;
		[FieldOffset(4)]
		int two;
		[FieldOffset(8)]
		int three;
	}
	static bool IsOSX ()
	{
		return (int)typeof (Environment).GetMethod ("get_Platform", BindingFlags.Static | BindingFlags.NonPublic).Invoke (null, null) == 6;
	}

	public unsafe static int Main () 
	{
		///
		///	Testing simple struct size
		///
		if(Marshal.SizeOf(typeof(TestStruct1)) != 32)
		{
			return 1;
		}

		TestStruct1 myStruct = new TestStruct1();
		myStruct.a = 0x12345678;

		IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TestStruct1)));
		Marshal.StructureToPtr(myStruct, p, false);

		Type testType = typeof(TestStruct1);
		if (Marshal.ReadInt32(p, (int)Marshal.OffsetOf(testType, "a")) != myStruct.a)
			return 2;

		Marshal.FreeHGlobal(p);

		///
		///	Testing struct size with ByValTStr string
		///
		if(Marshal.SizeOf(typeof(TestStruct2)) != 32)
			return 3;

		TestStruct2 myStruct2 = new TestStruct2();
		myStruct2.b = 0x12345678;

		p = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TestStruct2)));
		Marshal.StructureToPtr(myStruct2, p, false);

		Type testType2 = typeof(TestStruct2);
		if (Marshal.ReadInt32(p, (int)Marshal.OffsetOf(testType2, "b")) != myStruct2.b)
			return 4;

		Marshal.FreeHGlobal(p);

		///
		///	Test structure size and struct with inheritance
		///
		if(Marshal.SizeOf(typeof(TestStruct3)) != 64)
			return 5;

		TestStruct3 myStruct3 = new TestStruct3();
		myStruct3.b = 0x12345678;
		myStruct3.c = 0x76543210;
		p = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TestStruct3)));
		Marshal.StructureToPtr(myStruct3, p, false);

		Type testType3 = typeof(TestStruct3);
		
		if(Marshal.ReadInt32(p, (int)Marshal.OffsetOf(testType3, "b")) != myStruct3.b)
			return 6;

		if (Marshal.ReadInt32(p, (int)Marshal.OffsetOf(testType3, "c")) != myStruct3.c) 
			return 7;

		Marshal.FreeHGlobal(p);
		
		///
		///	Also make sure OffsetOf returns the correct Exception.
		///
		try {
			Marshal.OffsetOf(testType3, "blah");
			return 8;
		}
		catch(ArgumentException e)  {
			/// Way to go :)
		}
		catch(Exception e) {
			return 9;
		}

		// test size of structs with objects
		if (Marshal.SizeOf (typeof (TestStruct4)) != IntPtr.Size)
			return 10;
		if (Marshal.SizeOf (typeof (TestStruct5)) != IntPtr.Size)
			return 11;
		if (Marshal.SizeOf (typeof (TestStruct6)) != IntPtr.Size)
			return 12;
		// a VARIANT is 
		if (Marshal.SizeOf (typeof (TestStruct7)) != 16)
			return 13;
		if (IsOSX () && IntPtr.Size == 4) {
			if (Marshal.SizeOf (typeof (TestStruct8)) != 12)
				return 14;
			if (Marshal.SizeOf (typeof (TestStruct10)) != 12)
				return 16;
		} else {
			if (Marshal.SizeOf (typeof (TestStruct8)) != 16 && Marshal.SizeOf (typeof (TestStruct8)) != 12)
				return 14;
			if (Marshal.SizeOf (typeof (TestStruct10)) != 16 && Marshal.SizeOf (typeof (TestStruct10)) != 12)
				return 16;
		}
		if (Marshal.SizeOf (typeof (TestStruct9)) != 12)
			return 15;
		if (Marshal.SizeOf (typeof (TestStruct11)) != 11)
			return 17;
		if (Marshal.SizeOf (typeof (TestStruct12)) != 6)
			return 18;
		if (Marshal.SizeOf (typeof (TestStruct13)) != 12)
			return 19;
		if (Marshal.SizeOf (typeof (TestStruct14)) != 12)
			return 20;
		return 0;
	}
}

