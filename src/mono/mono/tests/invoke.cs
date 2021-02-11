using System;
using System.Reflection;

class Tests {
	
	public struct SimpleStruct {
		public bool a;
		public bool b;

		public SimpleStruct (bool arg) {
			a = arg;
			b = false;
		}
	}

	public static void Foo(ref int x, ref int y)
	{
		x = 20;
		y = 30;
	}
	
	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}

	public static int test_0_byref_null () {
		// Test that the objects for byref valuetype arguments are 
		// automatically created
		MethodInfo m3 = typeof(Tests).GetMethod("Foo");
		
		var args = new object[2];
		
		m3.Invoke(null, args);

		if ((((int)(args [0])) != 20) || (((int)(args [1])) != 30))
			return 2;

		return 0;
	}

	public static int test_0_ctor_vtype () {
		// Test the return value from  ConstructorInfo.Invoke when a precreated
		// valuetype is used.

		SimpleStruct ss = new SimpleStruct ();
		ss.a = true;
		ss.b = false;

		ConstructorInfo ci = typeof (SimpleStruct).GetConstructor (new Type [] { typeof (bool) });
		ci.Invoke (ss, new object [] { false });

		return 0;
	}

	public static int test_0_array_get_set () {
		// Test invoking of the array Get/Set methods
		string[,] arr = new string [10, 10];

		arr.GetType ().GetMethod ("Set").Invoke (arr, new object [] { 1, 1, "FOO" });
		string s = (string)arr.GetType ().GetMethod ("Get").Invoke (arr, new object [] { 1, 1 });
		if (s != "FOO")
			return 3;

		return 0;
	}

	public static int test_0_string_ctor_sharing () {
		// Test the sharing of runtime invoke wrappers for string ctors
		typeof (string).GetConstructor (new Type [] { typeof (char[]) }).Invoke (new object [] { new char [] { 'a', 'b', 'c' } });

		typeof (Assembly).GetMethod ("GetType", new Type [] { typeof (string), }).Invoke (typeof (int).Assembly, new object [] { "A" });
	
		return 0;
	}

	public static int test_0_ctor_delegate_argument_null () {

		var ci = typeof (Action).GetConstructor (new Type [] { typeof (object), typeof(IntPtr) });
		try {
			ci.Invoke (new object [] { new Tests(), IntPtr.Zero });
		} catch (TargetInvocationException ex) {
			if (ex.InnerException is ArgumentNullException)
				return 0;
		}

		return 1;
	}
}
