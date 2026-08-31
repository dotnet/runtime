using System;
using System.Reflection;
using System.Reflection.Emit;

public interface Parent {
	void Test ();
}

public interface ParentB {
	void TestB ();
}

public class Foo :  Parent, ParentB {
	public void Test () { Console.WriteLine ("Foo::Test"); }
	public void TestB () { Console.WriteLine ("Foo::TestB"); }
}

public class Bar :  Parent, ParentB {
	public void Test () { Console.WriteLine ("Bar::Test"); }
	public void TestB () { Console.WriteLine ("Bar::TestB"); }
}

class Driver {
	public static int Main (string[] args) {
		ParentB p;
		Foo f = new Foo();
		ParentB b = new Bar();
		p = args == null ? (ParentB) f : (ParentB) b;
		p.TestB();

		return 1;
	}
}	
