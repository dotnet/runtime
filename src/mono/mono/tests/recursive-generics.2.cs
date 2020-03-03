using System;

public class GenA<T> {};
public class GenB<T> : GenA<GenB<GenB<T>>> {};

public class GenC<T> {
	public object newA () {
		return new GenA<T> ();
	}
}

public class GenD<T> : GenC<GenD<GenD<T>>> {};

public class main {
	public static int Main () {
		GenB<string> gb = new GenB<string> ();
		GenD<string> gd = new GenD<string> ();

		gd.newA ();

		return 0;
	}
}
