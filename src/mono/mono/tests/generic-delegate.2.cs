using System;

public class ClassA {}

public delegate int IntDelegate (int x);
public delegate T[] TDelegate<T> ();

public class Gen<T> {
	public int intFunction (int x) {
		return x + 1;
	}

	public IntDelegate getIntDelegate () {
		return intFunction;
	}

	public virtual int virtIntFunction (int x) {
		return x + 2;
	}

	public IntDelegate getVirtIntDelegate () {
		return virtIntFunction;
	}

	public T[] tFunction () {
		return new T[3];
	}

	public TDelegate<T> getTDelegate () {
		return tFunction;
	}

	public static T[] staticTFunction () {
		return new T[3];
	}

	public TDelegate<T> getStaticTDelegate () {
		return staticTFunction;
	}
}

public class main {
	public static int Main () {
		Gen<ClassA> ga = new Gen<ClassA> ();
		IntDelegate id = ga.getIntDelegate ();
		TDelegate<ClassA> tda = ga.getTDelegate ();
		IntDelegate vid = ga.getVirtIntDelegate ();
		TDelegate<ClassA> stda = ga.getStaticTDelegate ();

		if (id (123) != 124)
			return 1;
		if (tda ().GetType () != typeof (ClassA[]))
			return 1;
		if (vid (123) != 125)
			return 1;
		if (stda ().GetType () != typeof (ClassA[]))
			return 1;

		tda = (TDelegate<ClassA>)Delegate.CreateDelegate (typeof (TDelegate<ClassA>),
				typeof (Gen<ClassA>).GetMethod ("staticTFunction"));

		if (tda ().GetType () != typeof (ClassA[]))
			return 1;

		return 0;
	}
}
