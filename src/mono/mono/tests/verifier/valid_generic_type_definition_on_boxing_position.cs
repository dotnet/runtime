using System;

public class Bar<T> {
	public int Z {get;set;}
}

public class Foo<T> {
	public T Test {get;set;}
	public int Z (Bar<T> t) {
		return t.Z;
	}
}

public struct Cat<T> {
	T t;
	public void Test () {
		Console.WriteLine (GetType ());
	}
}


class Driver {
	static void Main () {
		Cat<int> c = new Cat<int> ();
		c.Test ();
		new Foo<double> ().Z(new Bar<double>());
	}
}