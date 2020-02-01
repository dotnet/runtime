/* Bug #76671
   Note: gmcs currently emits duplicate TypeSpecs, so this
	 case doesn't get exposed, so use csc compiled
	 assemblies till gmcs is fixed.
*/

using System;

class X<T1> {
	public static void Xfoo () {
		Console.WriteLine (typeof (T1).ToString ());
	}
}

class Y<T2> {
	public static void Yfoo () {
		Console.WriteLine (typeof (T2).ToString ());
	}
}

class Test {
	static void Main ()
	{
	}
}
