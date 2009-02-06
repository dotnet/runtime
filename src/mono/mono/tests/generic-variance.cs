// Compiler options: -langversion:future

interface IFoo<out T> {
	T Bar { get; }
}

class Foo : IFoo<string> {
	readonly string bar;
	public Foo (string bar) {
		this.bar = bar;
	}
	public string Bar { get { return bar; } }
}

public class Test {
	static int Main () {
		string bar = "Who is John Galt?";
		IFoo<string> foo = new Foo(bar);
		IFoo<object> foo2 = foo;
		if (!foo2.Bar.Equals (bar))
			return 1;

		foo2 = new Foo(bar);
		if (foo2.Bar != bar)
			return 2;

		return 0;
	}
}
