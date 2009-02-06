// Compiler options: -langversion:future

delegate int Foo<in T> (T t);

public class Test {
	static int Main () {
		string message = "Who is John Galt?";
		Foo<object> foo = delegate (object o) { return o.GetHashCode (); } }
		Foo<string> foo2 = foo;
		if (foo2 (message) != message.GetHashCode ())
			return 1;
		return 0;
	}
}
