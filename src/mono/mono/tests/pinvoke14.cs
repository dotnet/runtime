using System;
using System.Runtime.InteropServices;

[StructLayout (LayoutKind.Sequential)]
class SimpleObj
{
	public string str;
	public int i;
}

class Test {
	
	[DllImport ("libtest")]
        static extern int class_marshal_test0 (SimpleObj obj);

	[DllImport ("libtest")]
        static extern void class_marshal_test1 (out SimpleObj obj);
	
	[DllImport ("libtest")]
        static extern int class_marshal_test2 (ref SimpleObj obj);

	[DllImport ("libtest")]
        static extern int class_marshal_test4 (SimpleObj obj);

	static int Main ()
	{
		SimpleObj obj0 = new SimpleObj ();
		obj0.str = "T1";
		obj0.i = 4;
		
		if (class_marshal_test0 (obj0) != 0)
			return 1;

		if (class_marshal_test4 (null) != 0)
			return 2;

		SimpleObj obj1;

		class_marshal_test1 (out obj1);

		if (obj1.str != "ABC")
			return 3;

		if (obj1.i != 5)
			return 4;

		if (class_marshal_test2 (ref obj1) != 0)
			return 5;
		
		return 0;
	}
}
