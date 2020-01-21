using System;

class Test {
	int i;

	int Foo ()
	{
		i = 3;
		return (i++) + (i++);
	}


	public static int Main ()
	{
		Test t = new Test ();
		if (t.Foo () != 7)
			return 1;

		return 0;
	}
}
