using System;
using System.Runtime.CompilerServices;

public class MyType {
	public MyType () {
		Method ();
	}

	[MethodImpl (MethodImplOptions.NoInlining)]
	public void Method () {
		var a = new OtherType ();
	}
		
}
