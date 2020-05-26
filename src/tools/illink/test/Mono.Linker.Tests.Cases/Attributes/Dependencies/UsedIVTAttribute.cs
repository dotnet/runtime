using System;
using System.Runtime.CompilerServices;

public class UsedInternalsVisibleToLib
{
	public static void TestA ()
	{
		Internals.CallStatic ();
	}
}
