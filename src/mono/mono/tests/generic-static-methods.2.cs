using System;

public class Gen<T> {
    public static T [] method<S> () {
	return new T [3];
    }
}

public class main {
    public static int Main () {
	if (Gen<object>.method<string> ().GetType () != typeof (object []))
	    return 1;
	return 0;
    }
}
