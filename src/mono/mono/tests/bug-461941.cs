using System;
using System.Reflection;


namespace TestApp
{
    class Driver
    {
		static int val;

		public static void Test (ref int? x) {
			val = x.Value;
		}

		static int Main()
		{
			MethodInfo mi = typeof (Driver).GetMethod ("Test");
			mi.Invoke (null, new object[] { 20 });
			if (val != 20)
				return 1;
			return 0;
		}
	}
}
