using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Gen<T> {
	public T[] method () {
		return new T[3];
	}

	public static T[] staticMethod () {
		return new T[3];
	}

	public S[] genericMethod<S> () {
		return new S[3];
	}
}

public class main {
	static bool callMethod<T> (Gen<T> g) {
		return g.method ().GetType () == typeof (T[]);
	}

	static bool callStaticMethod<T> () {
		return Gen<T>.staticMethod ().GetType () == typeof (T[]);
	}

	static bool callGenericMethod<T,S> (Gen<T> g) {
		return g.genericMethod<S> ().GetType () == typeof (S[]);
	}

	[MethodImpl (MethodImplOptions.NoInlining)]
	static bool work<T,S> () {
		Gen<T> g = new Gen<T> ();

		if (!callMethod<T> (g))
			return false;
		if (!callStaticMethod<T> ())
			return false;
		if (!callGenericMethod<T,S> (g))
			return false;
		return true;
	}

	public static int Main () {
		if (!work<string,string> ())
			return 1;
		if (!work<int,int> ())
			return 1;
		if (!work<string,int> ())
			return 1;
		if (!work<int,string> ())
			return 1;
		return 0;
	}
}
