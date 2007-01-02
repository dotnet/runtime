using System;
using System.Runtime.InteropServices;

public class Test 
{
	[DllImport ("libtest", EntryPoint="mono_test_byvalstr_gen")]
	public static extern IntPtr mono_test_byvalstr_gen();

	[DllImport ("libtest", EntryPoint="mono_test_byvalstr_check")]
	public static extern int mono_test_byvalstr_check(IntPtr data, string correctString);

	[DllImport ("libtest", EntryPoint="mono_test_byvalstr_check_unicode")]
	public static extern int mono_test_byvalstr_check_unicode(ref ByValStrStruct_Unicode var, int test);
	
	[StructLayout (LayoutKind.Sequential)]
	public struct ByValStrStruct 
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=100)]
		public string a;
	}

	[StructLayout (LayoutKind.Sequential, CharSet=CharSet.Unicode)]
	public struct ByValStrStruct_Unicode
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=4)]
		public string a;

		public int flag;
	}
	
	public unsafe static int Main () 
	{
		string testString = "A small string";

		IntPtr udata = mono_test_byvalstr_gen();

		ByValStrStruct data = new ByValStrStruct();
		data.a = testString;

		Marshal.StructureToPtr(data, udata, false);

		int c = mono_test_byvalstr_check(udata, testString);
		if (c != 0)
			return 1;

		ByValStrStruct_Unicode a = new ByValStrStruct_Unicode ();
		a.flag = 0x1234abcd;
		a.a = "1234";
		c = mono_test_byvalstr_check_unicode (ref a, 1);
		if (c != 0)
			return 2;

		a.a = "12";
		c = mono_test_byvalstr_check_unicode (ref a, 2);
		if (c != 0)
			return 3;

		a.a = "1234567890";
		c = mono_test_byvalstr_check_unicode (ref a, 3);
		if (c != 0)
			return 4;

		return 0;
	}
}

