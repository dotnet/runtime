using System;

public class Foo {
	public class Nested {

		class Inner {
			
		}
		
		public int Test () {
			return Bla<Inner[,]> ();
		}

		static int Bla<T> () { return 0; }
	}
}
class Program {

    static int Main (string[] args)
    {
		return new Foo.Nested ().Test ();
	}
}