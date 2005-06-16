using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

using Mono.Test;

#if RESTRICT
// this ensure we don't have FullTrust
[assembly: SecurityPermission (SecurityAction.RequestRefuse, SkipVerification = true)]
#endif

public class Program {

	static bool IsRestricted ()
	{
#if RESTRICT
		return true;
#else
		return false;
#endif
	}

	static int Main ()
	{
		try {
			// aptclib.dll is strongnamed and DOESN'T have [AllowPartiallyTrustedCallers]
			// so this call will work ONLY IF this assembly IS strongnamed
			string s = AptcLibrary.Hello ("World");
			int ec = IsRestricted () ? 1 : 0;
			Console.WriteLine ("*{0}* AptcLibrary: {1}", ec, s);
			return ec;
		}
		catch (SecurityException se) {
			int ec = IsRestricted () ? 0 : 1;
			Console.WriteLine ("*{0}* Expected SecurityException\n{1}", ec, se);
			return ec;
		}
		catch (Exception e) {
			Console.WriteLine ("*2* Unexpected exception\n{0}", e);
			return 2;
		}
	}
}
