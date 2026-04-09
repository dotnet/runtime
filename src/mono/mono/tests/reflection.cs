using System.Reflection;
using System.Collections;
using System;

namespace Test {
	internal class CM : IComparer {
		public int Compare (object x, object y) {
			return ((MethodInfo)x).Name.CompareTo (((MethodInfo)y).Name);
		}
	}
	public class T {
		public static int Main(string[] args) {
			string[] names = {
				"Equals", "Equals",
				"GetHashCode", "GetType",
				"ReferenceEquals", "ToString"
			};
			int i;
			string name = "System.Object";
			if (args.Length > 0)
				name = args [0];
			Type t = Type.GetType (name, true);
			MethodInfo[] ms = t.GetMethods();

			Array.Sort (ms, new CM());
			foreach (MethodInfo m in ms) {
				Console.WriteLine (m.ReturnType.Name + " " + m.Name);
			}
			if (name == "System.Object") {
				for (i=0; i < names.Length; ++i)
					if (names [i] != ms [i].Name)
						return i + 1;
			}
			return 0;
		}
	}
}
