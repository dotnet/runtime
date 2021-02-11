using System;

class Class1
{
	static void Main(string[] args)
	{
		Class1 o = new Class1();
		try {
		}
		finally {
			// this allocates space on the stack and
			// thus modifies the stack pointer
			decimal x = 7.7m;
		}
	}
}

