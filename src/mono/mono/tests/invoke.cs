using System;
using System.Reflection;

class Test {
	
	public struct SimpleStruct {
		public bool a;
		public bool b;
	}
	
	static void Test2 () {
		Console.WriteLine ("Test2 called");
	}
	
	public static SimpleStruct Test1 (SimpleStruct ss) {
		Console.WriteLine ("Test1 called " + ss.a + " " + ss.b);
		SimpleStruct res = new SimpleStruct ();
		res.a = !ss.a;
		res.b = !ss.b;
		return res;
	}

	public static void Foo(ref int x, ref int y)
	{
		x = 20;
		y = 30;
	}
	
	static int Main () {
		Type t = typeof (Test);

		MethodInfo m2 = t.GetMethod ("Test2");
		if (m2 != null)
			return 1;

		MethodInfo m1 = t.GetMethod ("Test1");
		if (m1 == null)
			return 1;

		object [] args = new object [1];
		SimpleStruct ss = new SimpleStruct ();
		ss.a = true;
		ss.b = false;
		args [0] = ss;
		
		SimpleStruct res = (SimpleStruct)m1.Invoke (null, args);

		if (res.a == true)
			return 1;
		if (res.b == false)
			return 1;

		// Test that the objects for byref valuetype arguments are 
		// automatically created
		MethodInfo m3 = typeof(Test).GetMethod("Foo");
		
		args = new object[2];
		
		m3.Invoke(null, args);

		if ((((int)(args [0])) != 20) || (((int)(args [1])) != 30))
			return 2;
	
		return 0;
	}
}
