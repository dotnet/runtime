using System;
using System.Security;

public class Program {

	static int Main ()
	{
		try {
			// mscorlib is strongnamed and has [AllowPartiallyTrustedCallers]
			// so this call will work even if this assembly isn't strongnamed
			Console.WriteLine ("*0* Hellp World");
			return 0;
		}
		catch (SecurityException se) {
			Console.WriteLine ("*1* Expected SecurityException\n{0}", se);
			return 1;
		}
		catch (Exception e) {
			Console.WriteLine ("*2* Unexpected exception\n{0}", e);
			return 2;
		}
	}
}
