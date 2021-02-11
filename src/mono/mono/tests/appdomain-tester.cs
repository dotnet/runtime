using System;
using System.Collections;
using System.Collections.Generic;

interface IConstraint {
	bool Eq (IConstraint b);
}

class Generic<A,B,C,D,E,F,G,H,T> where T: IConstraint
{
	public bool Eq (T a, T b) {
		var x = new List<T> ();
		x.Add (a);
		x.Add (b);
		Array.BinarySearch (x.ToArray (), b);
		a.Eq (b);
		return true;
	}
}

class Impl : IConstraint {
	public bool Eq (IConstraint b)  {
		return true;
	}
}

public class Unload2 {
	static void Main (string[] args) {
		var a = new Impl ();
		var b = new Generic<object, object, object, object, object, object, object, object, Impl> ();
		b.Eq (a, a);
	}
}