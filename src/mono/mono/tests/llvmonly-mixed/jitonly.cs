using System;
using System.Runtime.CompilerServices;

// Class whose methods are AOTed
public class JitOnly
{
	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void throw_ovf_ex () {
		throw new OverflowException ();
	}
}
