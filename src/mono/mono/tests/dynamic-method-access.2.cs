using System;
using System.Reflection;
using System.Reflection.Emit;

class Host {

        static int Field = 42;
}

class Program {

        delegate int Getter ();

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

                Console.WriteLine (g ());
		if (g () == 42)
			return 0;
		return 1;
        }
}
