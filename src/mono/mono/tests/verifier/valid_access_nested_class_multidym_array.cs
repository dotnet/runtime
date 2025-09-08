using System;

class MainClass
{
	class Foo {

	}

	public static void Main(string[] args)
	{
		Foo[,] f = new Foo [2,2];
		f[0,0] =new Foo ();
	}
}


