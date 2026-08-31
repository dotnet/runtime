using System;
using System.Collections.Generic;
using System.Reflection;

public class Tests {

	public static void NotNullItems<T>(IEnumerable<T> items) where T : class {
		foreach (object item in items) {
		}
	}

	public static void Main () {
		MethodBase[] arr = new ConstructorInfo[] { null, null };
		NotNullItems (arr);
	}
}
