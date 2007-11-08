using System;
using System.Reflection;
using System.Runtime.InteropServices;

class X {
        static int Main ()
        {
                ParameterInfo [] pi = typeof (X).GetMethod ("TakesInt").GetParameters ();

                Console.WriteLine ("{0} and {1}", pi [0].DefaultValue.GetType (), pi [1].DefaultValue.GetType ());
		if (pi [0].DefaultValue.GetType () != typeof (DBNull))
			return 1;
		if (pi [1].DefaultValue.GetType () != typeof (Missing))
			return 2;
		return 0;
        }

        public static void TakesInt (int b, [Optional] int a)
        {
        }
}
