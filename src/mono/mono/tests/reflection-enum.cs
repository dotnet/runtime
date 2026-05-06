using System.Reflection;
using System;

namespace Test {
	public class T {
		public static int Main(string[] args) {
			string defaultn = "System.Reflection.ParameterAttributes";
			string name = defaultn;
			int verbose = 0;
			foreach (string arg in args) {
				if (arg == "-v")
					verbose = 1;
				else
					name = arg;
			}
			Type t = Type.GetType (name);
			Array values = Enum.GetValues (t);
			string[] names = Enum.GetNames (t);
			int i;
		
			if (verbose != 0) {
				Console.WriteLine ("Enum "+t.Name);
				for (i = 0; i < names.Length; ++i) {
					Console.WriteLine ("{0} = {1} (ToString: {2})", names [i], ((int)values.GetValue(i)).ToString(), values.GetValue(i));
				}
			}
			if (name == defaultn) {
				string[] truenames = {"None", "In", "Out", "Lcid", "Retval",
					"Optional", "HasDefault", "HasFieldMarshal",
					"Reserved3", "Reserved4", "ReservedMask"};
				int[] truevalues = {0, 1, 2, 4, 8, 16, 4096, 8192,
					16384, 32768, 61440};

				for (i = 0; i < names.Length; ++i) {
					if (names [i] != truenames [i])
						return 1 + i;
					if ((int)values.GetValue (i) != truevalues [i])
						return 1 + names.Length + i;
				}
			}
			return 0;
		}
	}
}
