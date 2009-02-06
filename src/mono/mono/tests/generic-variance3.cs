// Compiler options: -langversion:future

delegate T Foo<out T> ();

public class Test {
	static int Main () {
		string message = "Who is John Galt?";
		Foo<string> foo = delegate () { return message; } }
		Foo<object> foo2 = foo;
		if (foo2 ().GetHashCode () != message.GetHashCode ())
			return 1;
		return 0;
	}
}
