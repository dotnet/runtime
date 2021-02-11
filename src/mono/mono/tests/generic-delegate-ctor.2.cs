using System;

public delegate R Function<R> ();

public struct GenStruct<T> {}

public class Gen<T> {
	public GenStruct<T> delFunc () {
		return default(GenStruct<T>);
	}

	public object makeDel () {
		return new Function<GenStruct<T>> (delFunc);
	}
}

public class main {
	public static int Main () {
		Gen<string> gs = new Gen<string> ();
		object del = gs.makeDel ();

		return 0;
	}
}
