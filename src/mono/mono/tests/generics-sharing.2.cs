using System;
using System.Collections.Generic;

public class ClassA {}
public class ClassB {}
public class ClassC {}

public class GenExc<T> : Exception {}

public class NonGen {
	public static int field = 123;

	public static void doThrow () {
		throw new GenExc<ClassA> ();
	}
}

public class GenA<T> {
	public static T[] arr;

	public static GenA () {
		arr = new T [3];
	}

	public GenA () {}

	public GenA<T> newGen () {
		return new GenA<T> ();
	}

	public GenA<int> newGenInt () {
		return new GenA<int> ();
	}

	public int getGenField () {
		return GenB<ClassA>.field;
	}

	public int getNonGenField () {
		return NonGen.field;
	}

	public T[] getArr () {
		return arr;
	}

	public T[] newArr () {
		return new T [3];
	}

	public GenA<T>[] newSelfArr () {
		return new GenA<T> [3];
	}

	public GenB<GenB<T>>[] newArrNested () {
		/*
		GenB<GenB<T>>[] arr = null;
		for (int i = 0; i < 10000000; ++i)
			arr = new GenB<GenB<T>> [3];
		*/
		return new GenB<GenB<T>> [3];
	}

	public int hash (T obj) {
		return obj.GetHashCode ();
	}

	public T ident (T obj) {
		return obj;
	}

	public T cast (Object obj) {
		return (T)obj;
	}

	public void except () {
		try {
			NonGen.doThrow();
		}
		catch (GenExc<T>)
		{
			//Console.WriteLine("exception thrown");
		}
	}
}

public class GenB<T> {
	public static int field = 123;
}

public class GenC<T> {
	public static int field ;

	public static GenC () {
		field = 1234;
	}
}

public class StaticTest<T> {
	static int stat;

	public StaticTest (int x) {
		stat = x;
	}

	public int getStat () {
		return stat;
	}

	public int getOtherStat () {
		return StaticTest<Object>.stat;
	}

	public int getGenCStat () {
		return GenC<T>.field;
	}
}

public class GenADeriv<T> : GenA<T> {
	public static int otherField = 123;
}

public class GenABDeriv<T> : GenA<GenB<T>> {
	public T[] newDerivArr () {
		return new T [3];
	}
}

public class NonGenUser<T> where T : NonGen {
	public int getNonGenField () {
		return T.field;
	}
}

public class AccessTest<T> {
	private static int field = 123;

	public int getOtherField () {
		return AccessTest<int>.field;
	}
}

public class VirtualTest<T> {
	public virtual T[] newArr () {
		return new T [3];
	}
}

public class VirtualTestDeriv<T> : VirtualTest<T> {
	public override T[] newArr () {
		return new T [4];
	}
}

public class VirtualTestCaller<T> {
	public T[] doCall (VirtualTest<T> vt) {
		return vt.newArr ();
	}
}

public class MyCons<T> {
	public T car;
	public MyCons<T> cdr;

	public static void printCar (T _car) {
		Console.WriteLine ("car " + _car.ToString () /* + " cdr " + _cdr.ToString () */);
	}

	public MyCons (T _car, MyCons<T> _cdr) {
		//printCar (_car);
		car = _car; cdr = _cdr;
	}

	public static MyCons<T> returnList (MyCons<T> l) { return l; }

	public static MyCons<T> returnCdr (MyCons<T> cons) { return returnList(cons.cdr); }
}

public class MyPair<N,M> {
	public N n;
	public M m;

	public MyPair (N _n, M _m) { n = _n; m = _m; }
}

public class MyDict<N,M> {
	public MyPair<N,M> p;

	public MyDict (N n, M m) { p = new MyPair<N,M> (n, m); }
}

public class RGCTXTest<T> {
	public GenA<T>[] newAArr () {
		return new GenA<T> [3];
	}
}

public class RGCTXTestSubA<T> : RGCTXTest<T> {
	public GenB<T>[] newBArr () {
		return new GenB<T> [3];
	}
}

public class RGCTXTestSubB<T> : RGCTXTest<T> {
	public GenC<T>[] newCArr () {
		return new GenC<T> [3];
	}
}

public class RGCTXTestSubASub : RGCTXTestSubA<ClassA> {
}

public class RGCTXTestSubASubSub<T> : RGCTXTestSubASub {
	public GenC<T>[] newCArr () {
		return new GenC<T> [3];
	}
}

public class main {
	static bool haveError = false;

	public static void error (string message) {
		haveError = true;
		Console.WriteLine (message);
	}

	public static void typeCheck (String method, Object obj, Type t) {
		if (obj.GetType () != t)
			error ("object from " + method + " should have type " + t.ToString () + " but has type " + obj.GetType ().ToString ());
	}

	public static void work<T> (T obj, bool mustCatch) {
		EqualityComparer<T> comp = EqualityComparer<T>.Default;

		GenA<T> ga = new GenA<T> ();

		typeCheck ("newGen", ga.newGen (), typeof (GenA<T>));
		typeCheck ("newGenInt", ga.newGenInt (), typeof (GenA<int>));
		typeCheck ("getArr", ga.getArr (), typeof (T[]));
		typeCheck ("newArr", ga.newArr (), typeof (T[]));
		typeCheck ("newSelfArr", ga.newSelfArr (), typeof (GenA<T>[]));
		//ga.newArrNested ();
		typeCheck ("newArrNested", ga.newArrNested (), typeof (GenB<GenB<T>>[]));

		if (ga.getGenField () != 123)
			error ("getGenField");

		if (ga.getNonGenField () != 123)
			error ("getNonGenField");

		ga.hash (obj);

		if (!comp.Equals (ga.ident (obj), obj))
			error ("ident");

		if (!comp.Equals (ga.cast (obj), obj))
			error ("cast");

		new GenADeriv<T> ();

		if (mustCatch) {
			bool didCatch = false;

			try {
				ga.except ();
			} catch (GenExc<ClassA>) {
				didCatch = true;
			}

			if (!didCatch)
				error ("except");
		} else
			ga.except ();

		MyDict<T, ClassB> dtb = new MyDict<T, ClassB> (obj, new ClassB ());

		typeCheck ("MyPair", dtb.p, typeof (MyPair<T, ClassB>));

		GenABDeriv<T> gabd = new GenABDeriv<T> ();

		typeCheck ("GenABDeriv.newArr", gabd.newArr (), typeof (GenB<T>[]));
		typeCheck ("GenABDeriv.newDerivArr", gabd.newDerivArr (), typeof (T[]));

		RGCTXTest<T> rt = new RGCTXTest<T> ();
		RGCTXTestSubA<T> rtsa = new RGCTXTestSubA<T> ();
		RGCTXTestSubB<T> rtsb = new RGCTXTestSubB<T> ();
		RGCTXTestSubASub rtsas = new RGCTXTestSubASub ();
		RGCTXTestSubASubSub<T> rtsass = new RGCTXTestSubASubSub<T> ();

		typeCheck ("rtsass.newCArr", rtsass.newCArr (), typeof (GenC<T>[]));
		typeCheck ("rgsa.newBArr", rtsa.newBArr (), typeof (GenB<T>[]));
		typeCheck ("rg.newAArr", rt.newAArr (), typeof (GenA<T>[]));
		typeCheck ("rgsb.newCArr", rtsb.newCArr (), typeof (GenC<T>[]));

		/* repeat all for each class */
		typeCheck ("rtsass.newCArr", rtsass.newCArr (), typeof (GenC<T>[]));
		typeCheck ("rtsass.newBArr", rtsass.newBArr (), typeof (GenB<ClassA>[]));
		typeCheck ("rtsass.newAArr", rtsass.newAArr (), typeof (GenA<ClassA>[]));

		typeCheck ("rtsas.newBArr", rtsas.newBArr (), typeof (GenB<ClassA>[]));
		typeCheck ("rtsas.newAArr", rtsas.newAArr (), typeof (GenA<ClassA>[]));

		typeCheck ("rtsa.newBArr", rtsa.newBArr (), typeof (GenB<T>[]));
		typeCheck ("rtsa.newAArr", rtsa.newAArr (), typeof (GenA<T>[]));

		typeCheck ("rtsb.newCArr", rtsb.newCArr (), typeof (GenC<T>[]));
		typeCheck ("rtsb.newAArr", rtsb.newAArr (), typeof (GenA<T>[]));

		typeCheck ("rt.newAArr", rt.newAArr (), typeof (GenA<T>[]));
	}

	public static void virtualTest<T> (VirtualTest<T> vt, int len) {
		VirtualTestCaller<T> vtc = new VirtualTestCaller<T> ();
		T[] arr = vtc.doCall (vt);

		typeCheck ("virtualTest", arr, typeof (T[]));

		if (arr.Length != len)
			error ("virtualTest length");
	}

	public static void listTest () {
		MyCons<string> ls = new MyCons<string> ("abc", null);
		MyCons<string> cdr = MyCons<string>.returnCdr (ls);

		if (cdr != null)
			error ("cdr is not null");
	}

	public static int Main ()
	{
		work<ClassA> (new ClassA (), false);
		work<ClassB> (new ClassB (), true);
		work<ClassC> (new ClassC (), true);
		work<GenA<ClassA>> (new GenA<ClassA> (), true);
		work<int[]> (new int[3], true);
		work<int> (123, true);

		StaticTest<ClassA> sa = new StaticTest<ClassA> (1234);
		StaticTest<ClassB> sb = new StaticTest<ClassB> (2345);

		if (sa.getStat () != 1234)
			error ("getStat");
		if (sb.getStat () != 2345)
			error ("getStat");
		if (sa.getOtherStat () != 0)
			error ("getOtherStat");
		if (sa.getGenCStat () != 1234)
			error ("getGenCStat A");
		if (sb.getGenCStat () != 1234)
			error ("getGenCStat B");

		NonGenUser<NonGen> ngu = new NonGenUser<NonGen> ();

		if (ngu.getNonGenField () != 123)
			error ("getNonGenField");

		AccessTest<ClassA> ata = new AccessTest<ClassA> ();

		if (ata.getOtherField () != 123)
			error ("getOtherField");

		VirtualTest<ClassA> vta = new VirtualTest<ClassA> ();
		VirtualTest<ClassB> vtb = new VirtualTestDeriv<ClassB> ();

		virtualTest<ClassA> (vta, 3);
		virtualTest<ClassB> (vtb, 4);

		listTest ();

		if (haveError)
			return 1;
		return 0;
	}
}
