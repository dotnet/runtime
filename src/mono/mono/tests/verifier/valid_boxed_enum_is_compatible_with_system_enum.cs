using System;

public enum TestEnum {
	A, B, C
}

class Driver {
	public static int Main (string[] args) {
		TestEnum te = TestEnum.A;
		te.ToString ();
		return 1;
	}
}
