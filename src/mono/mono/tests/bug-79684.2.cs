using System;
using System.Reflection;

namespace Tests
{
	public class Test
	{
	  	const BindingFlags flags = BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
		public static int Main()
		{
			foreach (MethodInfo m in typeof(Type[]).GetMethods(flags)) {
				if (m.Name == "System.Collections.Generic.IList`1.IndexOf") 	{
					Console.WriteLine(m.GetParameters().Length);
				}
			}
			return 0;
		}
	}
}
