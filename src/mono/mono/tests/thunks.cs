using System;
using System.Runtime.InteropServices;

public class Test
{
	[DllImport ("libtest")]
	public static extern int test_method_thunk (int id, IntPtr testMethodHandle,
		IntPtr createObjectHandle);

	static int test_method_thunk (int id, Type type)
	{
		string name = String.Format ("Foo{0}", id);
		return test_method_thunk (
			id,
			type.GetMethod (name).MethodHandle.Value,
			type.GetMethod ("CreateObject").MethodHandle.Value);
	}

	public static int Main ()
	{
		const int MaxClassTests = 13;
		const int MaxStructTests = 1;
		
		// tests of class "Test"
		for (int i = 0; i < MaxClassTests; i++) {
			int res = test_method_thunk (i, typeof (Test));
			if (res != 0)
				return i*10 + res;
		}

		// tests of struct "TestStruct"
		for (int i = 0; i < MaxStructTests; i++) {
			int res = test_method_thunk (MaxClassTests + i, typeof (TestStruct));
			if (res != 0)
				return i*10 + res;
		}
		
		return 0;
	}

	public static object CreateObject ()
	{
		Test t = new Test ();
		return t;
	}

	public static void Foo0 ()
	{
	}

	public static int Foo1 ()
	{
		return 42;
	}

	public static string Foo2 (string s)
	{
		return s;
	}

	public string Foo3 (string a)
	{
		return a;
	}

	public int Foo4 (string a, int i)
	{
		return i;
	}

	public int Foo5 (string a, int i)
	{
		throw new NotImplementedException ();
	}

	public bool Foo6 (byte a1, short a2, int a3, long a4, float a5, double a6, string a7)
	{
		return  a1 == 254 &&
			a2 == 32700 &&
			a3 == -245378 &&
			a4 == 6789600 &&
			(Math.Abs (a5 - 3.1415) < 0.001) &&
			(Math.Abs (a6 - 3.1415) < 0.001) &&
			a7 == "Foo6";
	}

	public static long Foo7 ()
	{
		return Int64.MaxValue;
	}

	public static void Foo8 (ref byte a1, ref short a2, ref int a3, ref long a4, ref float a5, ref double a6, ref string a7)
	{
		a1 = 254;
		a2 = 32700;
		a3 = -245378;
		a4 = 6789600;
		a5 = 3.1415f;
		a6 = 3.1415;
		a7 = "Foo8";
	}

	public static void Foo9 (ref byte a1, ref short a2, ref int a3, ref long a4, ref float a5, ref double a6, ref string a7)
	{
		throw new NotImplementedException ();
	}

	public static bool Foo10 (TestStruct s)
	{
		return s.A == 42 && Math.Abs (s.B - 3.1415) < 0.001;
	}

	public static void Foo11 (ref TestStruct s)
	{
		s.A = 42;
		s.B = 3.1415;
	}

	public static TestStruct Foo12 ()
	{
		TestStruct s = new TestStruct ();
		s.A = 42;
		s.B = 3.1415;
		return s;
	}
}


public struct TestStruct
{
	public int A;
	public double B;

	public static TestStruct CreateObject ()
	{
		return new TestStruct ();
	}

	public void Foo13 ()
	{
		A = 1;
		B = 17;
	}
}
