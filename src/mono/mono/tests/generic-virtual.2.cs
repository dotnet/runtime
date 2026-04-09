using System;

public class ClassA {}
public class ClassB {}

public delegate string[] StringArrayDelegate ();

public class Gen<T> {
	static bool checkArr<S> (Array arr, int length) {
		if (arr.GetType () != typeof (S[]))
			return false;
		if (arr.Length != length)
			return false;
		return true;
	}

	public bool test () {
		return checkArr<ClassB> (newArr<ClassB> (), myLength ());
	}

	public virtual int myLength () {
		return 3;
	}

	public virtual S[] newArr<S> () {
		return new S[3];
	}
}

public class GenSub<T> : Gen<T> {
	public override int myLength () {
		return 4;
	}

	public override S[] newArr<S> () {
		return new S[4];
	}
}

public class GenSubSub : GenSub<ClassA> {
	public override int myLength () {
		return 5;
	}

	public override S[] newArr<S> () {
		return new S[5];
	}

	public static S[] staticNewArr<S> () {
		return new S[5];
	}
}

public class main {
	public static int Main () {
		Gen<ClassA> ga = new Gen<ClassA> ();
		Gen<ClassA> gsa = new GenSub<ClassA> ();
		Gen<ClassA> gss = new GenSubSub ();
		int i;

		for (i = 0; i < 100; ++i) {
			if (!ga.test ())
				return 1;
			if (!gsa.test ())
				return 1;
			if (!gss.test ())
				return 1;

			StringArrayDelegate sad = new StringArrayDelegate (GenSubSub.staticNewArr<string>);
			string[] arr = sad ();
			if (arr.GetType () != typeof (string[]))
				return 1;
			if (arr.Length != 5)
				return 1;

			sad = new StringArrayDelegate (gss.newArr<string>);
			arr = sad ();
			if (arr.GetType () != typeof (string[]))
				return 1;
			if (arr.Length != 5)
				return 1;
		}

		/* A test for rebuilding generic virtual thunks */
		for (i = 0; i < 1000; ++i) {
			object o = ga.newArr<string> ();
			if (!(o is string[]))
				return 2;
		}
		for (i = 0; i < 1000; ++i) {
			object o = ga.newArr<object> ();
			if (!(o is object[]))
				return 2;
		}

		return 0;
	}
}
