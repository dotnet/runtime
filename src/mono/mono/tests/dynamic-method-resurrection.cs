using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

delegate int Getter ();

class Host {

	static int Field = 42;

	Getter g;

	public Host (Getter g) {
		this.g = g;
	}

	~Host () {
		Program.resed = g;
	}
}


class Program {
		internal static Getter resed;
		static int result;
		static void DoStuff ()
        {
			DynamicMethod method = new DynamicMethod ("GetField",
                        typeof (int), new Type [0], Type.GetType ("Host"));

			ILGenerator il = method.GetILGenerator ();
			il.Emit (OpCodes.Ldsfld, typeof (Host).GetField ("Field", BindingFlags.Static | BindingFlags.NonPublic));
			il.Emit (OpCodes.Ret);

			var g = (Getter)method.CreateDelegate (typeof (Getter));			
			new Host (g);
        }

		static bool CheckStuff () {
			if (resed == null)
				return false;
			Program.result = resed ();
			resed = null;
			return true;
		}

		public static int Main ()
        {
			int cnt = 5;
			DoStuff ();
			do {
				if (CheckStuff ())
					break;
				GC.Collect ();
				GC.WaitForPendingFinalizers ();
			} while (cnt-- > 0);
			GC.Collect ();
			GC.WaitForPendingFinalizers ();
			return result == 42 ? 0 : 1;
		}
}
