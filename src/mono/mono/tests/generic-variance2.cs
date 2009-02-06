// Compiler options: -langversion:future

interface IFoo<in T> {
	string Bar (T t);
}

class Foo : IFoo<object> {
	public string Bar (object t) {
		return t.GetType ().FullName;
	}
}

public class Test {
	static int Main () {
		IFoo<object> foo = new Foo ();
		IFoo<string> foo2 = foo;

		if (foo2.Bar ("blah") != typeof (string).FullName)
			return 1;

		foo2 = new Foo();
		if (foo2.Bar ("blah") != typeof (string).FullName)
			return 2;
		

		return 0;
	}
}
