using System;

public class GenExc<S,T> : Exception {
}

public delegate void ThrowDelegate ();

public class Gen<T> {
	public void catcher<S> (ThrowDelegate thrower) {
		try {
			thrower ();
		}
		catch (GenExc<S,T>) {
		}
	}

	public static void staticCatcher<S> (ThrowDelegate thrower) {
		try {
			thrower ();
		}
		catch (GenExc<S,T>) {
		}
	}
}

public class main {
	static void throwObjectObject () {
		throw new GenExc<object, object> ();
	}

	static void throwStringObject () {
		throw new GenExc<string, object> ();
	}

	static int Main () {
		Gen<object> go = new Gen<object> ();

		try {
			go.catcher<object> (new ThrowDelegate (main.throwObjectObject));
			Gen<object>.staticCatcher<object> (new ThrowDelegate (main.throwObjectObject));
			go.catcher<string> (new ThrowDelegate (main.throwStringObject));
			Gen<object>.staticCatcher<string> (new ThrowDelegate (main.throwStringObject));
		}
		catch {
			return 1;
		}

		try {
			go.catcher<object> (new ThrowDelegate (main.throwStringObject));
			return 1;
		}
		catch {
		}
		try {
			Gen<object>.staticCatcher<object> (new ThrowDelegate (main.throwStringObject));
			return 1;
		}
		catch {
		}

		try {
			go.catcher<string> (new ThrowDelegate (main.throwObjectObject));
			return 1;
		}
		catch {
		}
		try {
			Gen<object>.staticCatcher<string> (new ThrowDelegate (main.throwObjectObject));
			return 1;
		}
		catch {
		}

		return 0;
	}
}
