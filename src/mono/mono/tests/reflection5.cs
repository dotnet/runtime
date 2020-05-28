using System.Reflection;
using System;

public class T {
	public static int Main() {
		int[] arr = {1};
		Type t = arr.GetType ();
		Console.WriteLine ("type is: "+t.ToString());
		Console.WriteLine ("type is array: "+t.IsArray);
		if (t.ToString() != "System.Int32[]")
			return 1;
		if (!t.IsArray)
			return 2;
		return 0;
	}
}
