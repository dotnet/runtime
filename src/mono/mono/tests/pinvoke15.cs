using System;
using System.Runtime.InteropServices;

class Test {
	
	[DllImport ("libtest")]
        static extern int string_marshal_test0 (string str);

	[DllImport ("libtest")]
        static extern void string_marshal_test1 (out string str);

	[DllImport ("libtest")]
        static extern int string_marshal_test2 (ref string str);

	[DllImport ("libtest")]
        static extern int string_marshal_test3 (string str);

	static int Main ()
	{
		if (string_marshal_test0 ("TEST0") != 0)
			return 1;

		string res;
		
		string_marshal_test1 (out res);

		if (res != "TEST1")
			return 2;
		
		if (string_marshal_test2 (ref res) != 0)
			return 3;

		if (string_marshal_test3 (null) != 0)
			return 4;


		return 0;
	}
}
