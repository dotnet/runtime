using System;
using System.Reflection;
using System.Reflection.Emit;

delegate int Getter ();

class Host {

	static int Field = 42;

	Getter g;

	public Host (Getter g) {
		this.g = g;
	}

	~Host () {
		int d = g ();
		Console.WriteLine (d);
	}
}

class Program {

	static Host h;

        public static int Main ()
        {
			DynamicMethod method = new DynamicMethod ("GetField",
                        typeof (int), new Type [0], Type.GetType ("Host"));

			ILGenerator il = method.GetILGenerator ();
			il.Emit (OpCodes.Ldsfld, typeof (Host).GetField (
                        "Field", BindingFlags.Static |
BindingFlags.NonPublic));
			il.Emit (OpCodes.Ret);

			Getter g = (Getter) method.CreateDelegate (typeof (Getter));

			/* 
			 * Create an object whose finalizer calls a dynamic method which
			 * dies at the same time.
			 * Storing into a static guarantees that this is only finalized during
			 * shutdown. This is needed since the !shutdown case still doesn't
			 * work.
			 */
			h = new Host (g);

			return 0;
        }
}
