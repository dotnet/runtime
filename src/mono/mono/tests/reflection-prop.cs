using System.Reflection;
using System;

namespace Test {
	public class T {
		public static int Main(string[] args) {
			string name = "System.String";
			if (args.Length > 0)
				name = args [0];
			Type t = Type.GetType (name);
			PropertyInfo[] ms = t.GetProperties();

			foreach (PropertyInfo m in ms) {
				if (m.CanRead)
					Console.Write ("Type "+m.PropertyType.Name+" ");
				Console.WriteLine (m.Name);
			}
			return 0;
		}
	}
}
