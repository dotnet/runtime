using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

namespace System {

	public class Environment {

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static string internalGetGacPath ();
	}

	public class Program {

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
			try {
				string gac = Environment.internalGetGacPath ();
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
