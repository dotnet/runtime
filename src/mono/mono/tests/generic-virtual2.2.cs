public struct GenStruct<T> {
}

public class Gen<T> {
	public object doCall<S> (GenStruct<T> x) {
		return newArr (x);
	}

	public object newArr (GenStruct<T> x) {
		return new T [3];
	}
}

public class main {
	public static int Main () {
		Gen<string> gs = new Gen<string> ();

		if (gs.doCall<object> (new GenStruct<string> ()).GetType () != typeof (string []))
			return 1;
		return 0;
	}
}
