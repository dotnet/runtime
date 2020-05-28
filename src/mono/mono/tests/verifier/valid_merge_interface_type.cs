using System;
using System.Reflection;
using System.Reflection.Emit;

public interface Parent {
	void Test ();
}

public class Foo : Parent {
	public void Test () {}
}

public class Bar : Parent {
	public void Test () {}

}

class Driver {


	public static int Main (string[] args) {
		Parent p;
		Foo f = new Foo();
		Bar b = new Bar();
		p = args == null ? (Parent) f : (Parent) b;

		p.Test();

		return 1;
	}
}	
