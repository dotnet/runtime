using System;
using System.Reflection;
using System.Security;

namespace Test {

	public class Program {

		private const string icall = "internalGetGacPath";

		static bool IsEcmaSigned ()
		{
			byte[] pk = Assembly.GetExecutingAssembly ().GetName ().GetPublicKey ();
			if ((pk != null) && (pk.Length == 16) && (pk [8] == 0x04)) {
				int n = 0;
				for (int i=0; i < pk.Length; i++)
					n += pk [i];
				if (n == 4)
					return true;
			}
			return false;
		}

		static int Main ()
		{
			MethodInfo mi = typeof (System.Environment).GetMethod (icall, 
				BindingFlags.Static | BindingFlags.NonPublic);
			if (mi == null) {
				Console.WriteLine ("*3* Couldn't reflect on internalcall {0}", icall);
				return 3;
			}

			try {
				string gac = (string)mi.Invoke (null, null);
				int ec = IsEcmaSigned () ? 0 : 1;
				Console.WriteLine ("*{0}* internalGetGacPath: {1}", ec, gac);
				return ec;
			}
			catch (SecurityException se) {
				int ec = IsEcmaSigned () ? 1 : 0;
				Console.WriteLine ("*{0}* Expected SecurityException\n{1}", ec, se);
				return ec;
			}
			catch (Exception e) {
				Console.WriteLine ("*2* Unexpected exception\n{0}", e);
				return 2;
			}
		}
	}
}
