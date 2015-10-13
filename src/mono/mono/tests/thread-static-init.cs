using System;
using System.Runtime.InteropServices;

class Foo {
	[ThreadStatic]
	public static int foo;
}

class X {

        static int Main ()
        {
		Foo.foo = 1;
		new Foo ();
		Bar ();

		return Bug34598 ();
        }

	static int Bug34598 ()
	{
		if (Test.Zero.ToString () != "0")
			return 1;
		if (Test.One.ToString () != "1")
			return 2;
		if (Test.Two.ToString () != "2")
			return 3;

		if (Test2.Zero.ToString () != "0")
			return 4;
		if (Test2.One.ToString () != "1")
			return 5;
		if (Test2.Two.ToString () != "2")
			return 6;
		return 0;
	}
	
	static void Bar ()
	{
		Console.WriteLine (Foo.foo);
	}
}

[StructLayout(LayoutKind.Explicit)]
public struct Test
{
	public static float Zero = 0.0f;
	[ThreadStatic]
	public static float One = 1.0f;
	[ContextStatic]
	public static float Two = 2.0f;
}

public struct Test2
{
	public static float Zero = 0.0f;
	[ThreadStatic]
	public static float One = 1.0f;
	[ContextStatic]
	public static float Two = 2.0f;
}
