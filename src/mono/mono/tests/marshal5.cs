using System;
using System.Runtime.InteropServices;

public class Test 
{
	[DllImport ("libtest", EntryPoint="mono_test_byvalstr_gen")]
	public static extern IntPtr mono_test_byvalstr_gen();

	[DllImport ("libtest", EntryPoint="mono_test_byvalstr_check")]
	public static extern int mono_test_byvalstr_check(IntPtr data, string correctString);
	
	[StructLayout (LayoutKind.Sequential)]
	public struct ByValStrStruct 
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=100)]
		public string a;
	}
	
	public unsafe static int Main () 
	{
		string testString = "A small string";

		IntPtr udata = mono_test_byvalstr_gen();

		ByValStrStruct data = new ByValStrStruct();
		data.a = testString;

		Marshal.StructureToPtr(data, udata, false);

		return mono_test_byvalstr_check(udata, testString);
	}
}

