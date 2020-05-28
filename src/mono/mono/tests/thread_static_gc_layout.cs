using System;

public struct Sandwich
{
	public IntPtr a;
	public object b;
	public IntPtr c;
	public IntPtr d;
}

class Driver {
	[ThreadStatic]
	static Sandwich blt;
	// const long initial_val = 0x0100000001L;
	const int initial_val = 1;

	static int Main ()
	{
		blt.a = (IntPtr)initial_val;
		blt.b = new object ();
		blt.c = (IntPtr)initial_val;
		blt.d = (IntPtr)initial_val;
		GC.Collect ();
		return (blt.a == blt.c && blt.c == blt.d && blt.a == (IntPtr)initial_val) ? 0 : -1;
	}
}