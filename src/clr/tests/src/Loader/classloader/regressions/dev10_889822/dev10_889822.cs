using System;
using System.IO;

interface IFoo {
    void foo();
}

class My {
	static int Main()
	{
		try {
		   IFoo s = null;
		   s.foo();		   
		}
		catch (NullReferenceException) {
			Console.WriteLine("Successfully caught a null reference exception.");
			return 100;
		}
		
		Console.WriteLine("Failed!!");
		return -1;
	}
}
