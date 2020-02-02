using System;
using System.Collections.Generic;

public struct GenStruct<T> {
    public int inc (int i) {
	return i + 1;
    }

    public T[] newArr () {
	return new T [3];
    }
}

public class Bla {
    public bool work (IList<string> l) {
	foreach (string s in l) {
	    if (s.Length != 1)
		return false;
	}
	return true;
    }
}

public interface GenInterface<T> {
    T[] newArr ();
}

public struct GenIntStruct<T> : GenInterface<T> {
    public T[] newArr () {
	return new T [3];
    }
}

public interface GenFactory<T> {
    GenInterface<T> makeInterface ();
}

public class Gen<T> : GenFactory<T> {
    public GenInterface<T> makeInterface () {
	return new GenIntStruct<T> ();
    }
}

public class NonGen : GenFactory<string> {
    public GenInterface<string> makeInterface () {
	return new GenIntStruct<string> ();
    }
}

public class main {
    public static bool testInterface (GenFactory<string> gf) {
	GenInterface<string> gi = gf.makeInterface ();
	if (gi.newArr ().GetType () != typeof (string []))
	    return false;
	return true;
    }

    public static int Main () {
	GenStruct<object> gso = new GenStruct<object> ();
	GenStruct<string> gss = new GenStruct<string> ();

	if (gso.inc (1) != 2)
	    return 1;
	if (gss.inc (2) != 3)
	    return 1;

	if (gso.newArr ().GetType () != typeof (object []))
	    return 1;
	if (gss.newArr ().GetType () != typeof (string []))
	    return 1;

	Gen<string> g = new Gen<string> ();
	testInterface (g);

	NonGen ng = new NonGen ();
	testInterface (ng);

	Bla bla = new Bla ();
	string [] arr = { "a", "b", "c" };

	if (!bla.work (arr))
	    return 1;

	return 0;
    }
}
