using System;
using System.Reflection;
//using System.Runtime.CompilerServices;
using System.Security;

using Mono.Test;

public class Program {

	static bool IsSigned ()
	{
		return (Assembly.GetExecutingAssembly ().GetName ().GetPublicKey () != null);
	}

	static int Main ()
	{
		try {
			// aptclib.dll is strongnamed and DOESN'T have [AllowPartiallyTrustedCallers]
			// so this call will work ONLY IF this assembly IS strongnamed
			string s = AptcLibrary.Hello ("World");
			int ec = IsSigned () ? 0 : 1;
			Console.WriteLine ("*{0}* AptcLibrary: {1}", ec, s);
			return ec;
		}
		catch (SecurityException se) {
			int ec = IsSigned () ? 1 : 0;
			Console.WriteLine ("*{0}* Expected SecurityException\n{1}", ec, se);
			return ec;
		}
		catch (Exception e) {
			Console.WriteLine ("*2* Unexpected exception\n{0}", e);
			return 2;
		}
	}
}
