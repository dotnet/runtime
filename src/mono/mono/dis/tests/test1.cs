/* Bug #76671
   Note: gmcs currently emits duplicate TypeSpecs, so this
	 case doesn't get exposed, so use csc compiled
	 assemblies till gmcs is fixed.
*/

class X<T1> {
	public static void Xfoo () {
		X<T1>.Xfoo();
	}
}

class Y<T2> {
	public static void Yfoo () {
		X<T2>.Xfoo();
	}
}

class Test {
	static void Main ()
	{
	}
}
