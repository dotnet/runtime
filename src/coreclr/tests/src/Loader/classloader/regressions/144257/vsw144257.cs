// this is regression test for VSW1 144257
// Loading type C resulted in TypeLoadException

using System;

interface I
{
	void meth();
}

class A
{
	public void meth(){}
}

class B : A
{
	new private void meth(){}
}

class C : B, I
{
	public static int Main()
	{
		try
		{
			C c = new C();
			Console.WriteLine("PASS");
			return 100;
		}
		catch (Exception e)
		{
			Console.WriteLine("Caught unexpected exception: " + e);
			return 101;
		}

	}
}