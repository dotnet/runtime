using System;
using System.Runtime.InteropServices;

[StructLayout (LayoutKind.Sequential)]
struct winx64_struct1
{
	public byte a;
}

[StructLayout (LayoutKind.Sequential)]
struct winx64_struct2
{
	public byte a;
	public byte b;
}

[StructLayout (LayoutKind.Sequential)]
struct winx64_struct3
{
	public byte a;
	public byte b;
	public short c;
}

[StructLayout (LayoutKind.Sequential)]
struct winx64_struct4
{
	public byte a;
	public byte b;
	public short c;
	public uint d;
}

[StructLayout (LayoutKind.Sequential)]
struct winx64_struct5
{
	public byte a;
	public byte b;
	public byte c;
}

[StructLayout (LayoutKind.Sequential)]
struct winx64_floatStruct
{
	public float a;
	public float b;
}

[StructLayout (LayoutKind.Sequential)]
struct winx64_doubleStruct
{
	public double a;
}

class winx64structs
{
	[DllImport ("libtest")]
	static extern int test_Winx64_struct1_in ([MarshalAs (UnmanagedType.Struct)] winx64_struct1 var);

	[DllImport ("libtest")]
	static extern int test_Winx64_struct2_in ([MarshalAs (UnmanagedType.Struct)] winx64_struct2 var);

	[DllImport ("libtest")]
	static extern int test_Winx64_struct3_in ([MarshalAs (UnmanagedType.Struct)] winx64_struct3 var);

	[DllImport ("libtest")]
	static extern int test_Winx64_struct4_in ([MarshalAs (UnmanagedType.Struct)] winx64_struct4 var);

	[DllImport ("libtest")]
	static extern int test_Winx64_struct5_in ([MarshalAs (UnmanagedType.Struct)] winx64_struct5 var);

	[DllImport ("libtest")]
	static extern int test_Winx64_structs_in1 ([MarshalAs (UnmanagedType.Struct)] winx64_struct1 var1,
						   [MarshalAs (UnmanagedType.Struct)] winx64_struct2 var2,
						   [MarshalAs (UnmanagedType.Struct)] winx64_struct3 var3,
						   [MarshalAs (UnmanagedType.Struct)] winx64_struct4 var4);

	[DllImport ("libtest")]
	static extern int test_Winx64_structs_in2 ([MarshalAs (UnmanagedType.Struct)] winx64_struct1 var1,
						   [MarshalAs (UnmanagedType.Struct)] winx64_struct1 var2,
						   [MarshalAs (UnmanagedType.Struct)] winx64_struct1 var3,
					           [MarshalAs (UnmanagedType.Struct)] winx64_struct1 var4,
						   [MarshalAs (UnmanagedType.Struct)] winx64_struct1 var6);

	[DllImport ("libtest")]
	[return:MarshalAs (UnmanagedType.Struct)]
	static extern winx64_struct1 test_Winx64_struct1_ret ();

	[DllImport ("libtest")]
	[return: MarshalAs (UnmanagedType.Struct)]
	static extern winx64_struct2 test_Winx64_struct2_ret ();

	[DllImport ("libtest")]
	[return: MarshalAs (UnmanagedType.Struct)]
	static extern winx64_struct3 test_Winx64_struct3_ret ();

	[DllImport ("libtest")]
	[return: MarshalAs (UnmanagedType.Struct)]
	static extern winx64_struct4 test_Winx64_struct4_ret ();

	[DllImport ("libtest")]
	static extern int test_Winx64_floatStruct ([MarshalAs (UnmanagedType.Struct)] winx64_floatStruct var);

	[DllImport ("libtest")]
	static extern int test_Winx64_doubleStruct ([MarshalAs (UnmanagedType.Struct)] winx64_doubleStruct var);

	private static bool enableBroken = false;
	
	static int Main (string[] args)
	{
		int testCode;
		bool haveError = false;

		if ((testCode = Test_In_Args_Value_In_RCX ()) != 0)
			Console.WriteLine ("Test_In_Args_Value_In_RCX Failed with code {0}",
				testCode, haveError = true);

		if ((testCode = Test_In_Args_Value_On_Stack_ADDR_In_RCX ()) != 0)
		        Console.WriteLine ("Test_In_Args_Value_On_Stack_ADDR_In_RCX Failed with code {0}",
		                testCode, haveError = true);

		if ((testCode = Test_In_Args_Values_In_Multiple_Registers ()) != 0)
			Console.WriteLine ("Test_In_Args_Values_In_Multiple_Registers Failed with code {0}",
				testCode, haveError = true);

		if ((testCode = Test_Ret_In_RAX ()) != 0)
			Console.WriteLine ("Test_Ret_In_RAX Failed with code {0}",
				testCode, haveError = true);

		if ((testCode = Test_In_Args_Values_In_Registers_and_Stack ()) != 0)
			Console.WriteLine ("Test_In_Args_Values_In_Registers_and_Stack Failed with code {0}",
				testCode, haveError = true);
		
		if (haveError)
			return 1;

		return 0;
	}

	static int Test_In_Args_Value_In_RCX ()
	{
		int retCode;

		winx64_struct1 t_winx64_struct1 = new winx64_struct1 ();
		t_winx64_struct1.a = 123;

		if ((retCode = test_Winx64_struct1_in (t_winx64_struct1)) != 0)
			return 100 + retCode;

		winx64_struct2 t_winx64_struct2 = new winx64_struct2 ();
		t_winx64_struct2.a = 4;
		t_winx64_struct2.b = 5;

		if ((retCode = test_Winx64_struct2_in (t_winx64_struct2)) != 0)
			return 200 + retCode;

		winx64_struct3 t_winx64_struct3 = new winx64_struct3 ();
		t_winx64_struct3.a = 4;
		t_winx64_struct3.b = 5;
		t_winx64_struct3.c = 0x1234;

		if ((retCode = test_Winx64_struct3_in (t_winx64_struct3)) != 0)
			return 300 + retCode;

		winx64_struct4 t_winx64_struct4 = new winx64_struct4 ();
		t_winx64_struct4.a = 4;
		t_winx64_struct4.b = 5;
		t_winx64_struct4.c = 0x1234;
		t_winx64_struct4.d = 0x87654321;

		if ((retCode = test_Winx64_struct4_in (t_winx64_struct4)) != 0)
			return 400 + retCode;

		winx64_floatStruct t_winx64_floatStruct = new winx64_floatStruct ();
		t_winx64_floatStruct.a = 5.5F;
		t_winx64_floatStruct.b = 9.5F;

		if ((retCode = test_Winx64_floatStruct (t_winx64_floatStruct)) != 0)
			return 500 + retCode;

		winx64_doubleStruct t_winx64_doubleStruct = new winx64_doubleStruct ();
		t_winx64_doubleStruct.a = 5.5F;

		if ((retCode = test_Winx64_doubleStruct (t_winx64_doubleStruct)) != 0)
			return 600 + retCode;

		return 0;
	}

	static int Test_In_Args_Values_In_Multiple_Registers ()
	{
		int retCode; 
		
		winx64_struct1 t_winx64_struct1 = new winx64_struct1 ();
		t_winx64_struct1.a = 123;

		winx64_struct2 t_winx64_struct2 = new winx64_struct2 ();
		t_winx64_struct2.a = 4;
		t_winx64_struct2.b = 5;

		winx64_struct3 t_winx64_struct3 = new winx64_struct3 ();
		t_winx64_struct3.a = 4;
		t_winx64_struct3.b = 5;
		t_winx64_struct3.c = 0x1234;

		winx64_struct4 t_winx64_struct4 = new winx64_struct4 ();
		t_winx64_struct4.a = 4;
		t_winx64_struct4.b = 5;
		t_winx64_struct4.c = 0x1234;
		t_winx64_struct4.d = 0x87654321;

		if ((retCode = test_Winx64_structs_in1 (t_winx64_struct1, t_winx64_struct2,
							t_winx64_struct3, t_winx64_struct4)) != 0)
			return 100 + retCode;

		
		return 0;
	}

	static int Test_Ret_In_RAX ()
	{
		winx64_struct1 t_winx64_struct1 = test_Winx64_struct1_ret ();
		if (t_winx64_struct1.a != 123)
			return 101;

		winx64_struct2 t_winx64_struct2 = test_Winx64_struct2_ret ();
		if (t_winx64_struct2.a != 4)
			return 201;
		if (t_winx64_struct2.b != 5)
			return 202;

		winx64_struct3 t_winx64_struct3 = test_Winx64_struct3_ret ();
		if (t_winx64_struct3.a != 4)
			return 301;
		if (t_winx64_struct3.b != 5)
			return 302;
		if (t_winx64_struct3.c != 0x1234)
			return 303;
		
		winx64_struct4 t_winx64_struct4 = test_Winx64_struct4_ret ();
		if (t_winx64_struct4.a != 4)
			return 401;
		if (t_winx64_struct4.b != 5)
			return 402;
		if (t_winx64_struct4.c != 0x1234)
			return 403;
		if (t_winx64_struct4.d != 0x87654321)
			return 404;

		return 0;
	}

	static int Test_In_Args_Values_In_Registers_and_Stack ()
	{
		int retCode;

		winx64_struct1 var1 = new winx64_struct1 ();
		var1.a = 1;
		winx64_struct1 var2 = new winx64_struct1 ();
		var2.a = 2;
		winx64_struct1 var3 = new winx64_struct1 ();
		var3.a = 3;
		winx64_struct1 var4 = new winx64_struct1 ();
		var4.a = 4;
		winx64_struct1 var5 = new winx64_struct1 ();
		var5.a = 5;

		if ((retCode = test_Winx64_structs_in2 (var1, var2, var3, var4, var5)) != 0)
			return 100 + retCode;

		return 0;
	}

	static int Test_In_Args_Value_On_Stack_ADDR_In_RCX ()
	{
		int retCode;

		if (enableBroken) {
			winx64_struct5 t_winx64_struct5 = new winx64_struct5 ();
			t_winx64_struct5.a = 4;
			t_winx64_struct5.b = 5;
			t_winx64_struct5.c = 6;

			if ((retCode = test_Winx64_struct5_in (t_winx64_struct5)) != 0)
				return 100 + retCode;
		}

		return 0;
	}
}