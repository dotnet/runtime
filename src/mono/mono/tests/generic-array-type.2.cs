using System;

public class Gen<T> {
	public Type arrayType () {
		return typeof (T []);
	}
}

public class main {
	public static int Main () {
		Gen<string> gs = new Gen<string> ();

		if (gs.arrayType () != typeof (string []))
			return 1;
		return 0;
	}
}
