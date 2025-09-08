using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public static class Driver
{
	public static void Test ()
	{
		Console.WriteLine ("x",1,2,3,4,__arglist (1,2,3));
		"ff".Substring (1,1);
	}

	public static void Main()//keep it as last method in the file
	{
	}
}


