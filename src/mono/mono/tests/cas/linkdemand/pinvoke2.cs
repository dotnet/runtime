using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System {

	public class Program {

                [DllImport ("libc", SetLastError=true)]
                public static extern uint getuid ();

		// this attribute has NO effect on LinkDemand!
		[SuppressUnmanagedCodeSecurity]
		static int Test ()
		{
			try {
				uint uid = getuid ();
				Console.WriteLine ("*0* getuid: {0}", uid);
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

		static int Main ()
		{
			return Test ();
		}
	}
}
