using System;

class Foo {
	[ThreadStatic]
	public static int foo;
}

class X {

        static void Main ()
        {
		Foo.foo = 1;
		new Foo ();
		Bar ();
        }
	
	static void Bar ()
	{
		Console.WriteLine (Foo.foo);
	}
}
