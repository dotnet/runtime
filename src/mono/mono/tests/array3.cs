

using System;


public class Test {


	public static int Main () {
		object[] array = new object[10];

		if (array.GetType ().IsPublic)
			return 0;

		return 1;
	}

}

