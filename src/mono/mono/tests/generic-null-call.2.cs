using System;

public struct GenStruct<T> {
    public int a;
    public T b;
}

public class Gen<T> {
    public virtual S[] newArr<S> (int i) {
	return new S [i];
    }
}

public class GenSub<T> : Gen<T> {
    public sealed override S[] newArr<S> (int i) {
	return new S [i+1];
    }
}

public class main {
    public static int Main () {
	GenSub<string> gst = null;

	try {
	    gst.newArr<object> (3);
	} catch (NullReferenceException) {
	    return 0;
	}

	return 1;
    }
}
