using System;

public class Tests {

	struct A1 {
		long a1, a2, a3, a4;
	}

	struct A2 {
		A1 a1, a2, a3, a4;
	}

	struct A3 {
		A2 a1, a2, a3, a4;
	}

	struct A4 {
		A3 a1, a2, a3, a4;
	}

	struct A5 {
		A4 a1, a2, a3, a4;
	}

	public static int foo () {
		A5 a5;

		return foo () + 1;
	}

	public static int Main () {
		try {
			foo ();
		}
		catch (StackOverflowException) {
			Console.WriteLine ("Stack overflow caught.");
			return 0;
		}
		return 1;
	}
}
