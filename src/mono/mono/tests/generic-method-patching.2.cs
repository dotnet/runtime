using System;
using System.Collections.Generic;

public class MyDict<S,T> {
    public void Add (S key, T value) {
	S[] sa = new S[1];
	T[] ta = new T[1];

	sa[0] = key;
	ta[0] = value;
    }
}

public abstract class FastFunc<S,T> {
    public abstract S Invoke (T bla);
}

public class StringFastFunc : FastFunc<string, int> {
    public override string Invoke (int bla) {
	return bla.ToString ();
    }
}

public class ArrayFastFunc : FastFunc<byte [], int> {
    public override byte [] Invoke (int bla) {
	return new byte [bla];
    }
}

public class IntCache<T> {
    MyDict<int,T> cache;

    public T Invoke (FastFunc<T,int> f, int bla) {
	if (cache == null)
	    cache = new MyDict <int,T> ();

	T value = f.Invoke (bla);

	cache.Add (bla, value);

	return value;
    }
}

public class main {
    public static int Main () {
	StringFastFunc sff = new StringFastFunc ();
	ArrayFastFunc aff = new ArrayFastFunc ();
	IntCache<string> ics = new IntCache<string> ();
	MyDict<string,string> dss = new MyDict<string,string> ();

	dss.Add ("123", "456");

	ics.Invoke (sff, 123);
	ics.Invoke (sff, 456);

	IntCache<byte []> ica = new IntCache<byte []> ();

	ica.Invoke (aff, 1);
	ica.Invoke (aff, 2);
	ica.Invoke (aff, 3);

	return 0;
    }
}
