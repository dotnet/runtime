using System;

public struct sa {
	public int a;
	public int b;
	public int c;
	public int d;
}

public struct sb {
	public sa a;
	public sa b;
	public sa c;
	public sa d;
}

public struct sc {
	public sb a;
	public sb b;
	public sb c;
	public sb d;
}

public struct sd {
	public sc a;
	public sc b;
	public sc c;
	public sc d;
}

public struct se {
	public sd a;
	public sd b;
	public sd c;
	public sd d;
}

public class main {
	public static int heusl (se x) {
		Console.WriteLine ("within");
		return 123;
	}

	public static void thrower () {
		try {
			throw new Exception ();
		} finally {
			Console.WriteLine ("before");
			heusl (new se ());
			Console.WriteLine ("after");
		}
	}

	public static int Main () {
		try {
			thrower ();
		} catch {
			Console.WriteLine ("back");
		}
		return 0;
	}
}
