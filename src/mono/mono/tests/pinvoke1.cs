using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public class T {

	public virtual object MyClone ()
	{
		return null;
	}

}

public class Test : T {

	[MethodImplAttribute(MethodImplOptions.InternalCall)]
	public override extern object MyClone ();

	delegate int MyDelegate (string name);

	[DllImport ("libtest", EntryPoint="mono_test_puts_static")]
	public static extern int puts_static (string name);

	public static int Main () {
		puts_static ("A simple Test for PInvoke 1");

		MyDelegate d = new MyDelegate (puts_static);
		d ("A simple Test for PInvoke 2");

		object [] args = {"A simple Test for PInvoke 3"};
		d.DynamicInvoke (args);
		
		int noimpl = 0;
		try {
			Test X = new Test ();
			X.MyClone ();
		} catch {
			noimpl = 1;
		}

		if (noimpl == 0)
			return 1;
		
		return 0;
	}
}
