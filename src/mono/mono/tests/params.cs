using System;

public class T {

	static public void method (int nargs, string arg) {
		int i;
		Console.WriteLine ("Got single arg "+arg);
	}
	static public void method (int nargs, params string[] args) {
		int i;
		Console.Write ("Got "+nargs.ToString()+" args ");
		Console.WriteLine ("("+args.Length.ToString()+"):");
		for (i = 0; i < nargs; ++i)
			Console.WriteLine (args [i]);
	}
	public static int Main() {
		method (1, "hello");
		method (2, "hello", "World");
		method (3, "hello", "World", "blah");
		return 0;
	}
}
