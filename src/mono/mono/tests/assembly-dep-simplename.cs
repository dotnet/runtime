using System;

using System.Runtime.CompilerServices;

// This class references "AClass.X" which comes from
// assembly-load-dir1/LibSimpleName.dll at compile time (see the Makefile)
// but which will be resolved by assembly-load-dir2/libsimplename.dll at runtime.
public class MidClass
{
	public MidClass ()
	{
		X = Foof ();
	}

	// The NoInlining here is an attempt to control precisely when the
	// reference to the LibSimpleName assembly is going to be resolved.
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static int Foof()
	{
		var a = new AClass ();
		return a.X;
	}

	public int X;
}

