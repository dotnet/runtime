using System;

class A : T {}
class T {
	static int Main ()
	{
		object o = (T [][]) (object) (new A [][] {});
		return o.GetHashCode () - o.GetHashCode ();
	}
}

