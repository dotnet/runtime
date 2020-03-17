using System;
using System.Collections.Generic;
using System.Reflection;

namespace GenericSharingTest {

public delegate int IntVoidDelegate ();

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

public class GenBi<S,T> {
	public static int field = 123;
	public static float floatField = 1.0f;

	public static int staticMethod (int x) {
		return x + field;
	}

	public static void staticVoidMethod (int x) {
		field = x;
	}

	public static float staticFloatMethod () {
		return floatField;
	}

	public static long staticLongMethod (long x) {
		return x + field;
	}

	public static GenStruct<T> staticValueMethod (int x) {
		return new GenStruct<T> (x);
	}
}

public struct GenStruct<T> {
	public static int staticField;

	public int field;
	public int dummy1;
	public int dummy2;
	public int dummy3;

	public GenStruct (int f) {
		field = f;
		dummy1 = dummy2 = dummy3 = 0;
	}

	public int method (int x) {
		return x + field;
	}
}

public interface IGen<T> {
	T[] iMethod ();
	void voidIMethod (int x);
	long longIMethod (long x);
	float floatIMethod ();
	GenStruct<T> valueIMethod (int x);
}

public class IGenImpl<T> : IGen<T> {
	public int field;

	public T[] iMethod () {
		return new T[3];
	}

	public void voidIMethod (int x) {
		field = x;
	}

	public long longIMethod (long x) {
		return x + 1;
	}

	public float floatIMethod () {
		return 1.0f;
	}

	public GenStruct<T> valueIMethod (int x) {
		return new GenStruct<T> (x);
	}
}

public class GenA<T> {
	public static T[] arr;

	static GenA () {
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

	public int getGenStructStaticField () {
		return GenStruct<T>.staticField;
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

	public GenStruct<T> structCast (Object obj) {
		return (GenStruct<T>)obj;
	}

	public Type ldtokenT () {
		return typeof (T);
	}

	public Type ldtokenIGenT () {
		return typeof (IGen<T>);
	}

	public Type ldtokenGenAIGenT () {
		return typeof (GenA<IGen<T>>);
	}

	public Type ldtokenGenB () {
		return typeof (GenB<>);
	}

	public GenStruct<T>? makeNullable (Object obj) {
		return (GenStruct<T>?)obj;
	}

	public object unmakeNullable (GenStruct<T>? obj) {
		return (object)obj;
	}

	public void except () {
		try {
			NonGen.doThrow ();
		}
		catch (GenExc<T>) {
			//Console.WriteLine("exception thrown");
		}
	}

	public static void staticExcept () {
		try {
			NonGen.doThrow ();
		}
		catch (GenExc<T>) {
			Console.WriteLine("exception thrown and caught");
		}
	}

	public static int staticField = 54321;

	public static int staticMethod () {
		return staticField;
	}

	public static int staticMethodCaller () {
		return staticMethod ();
	}

	public static float staticFloatField = 1.0f;

	public static float staticFloatMethod () {
		return staticFloatField;
	}

	public static int staticBiCaller (int x) {
		return GenBi<int,T>.staticMethod (x);
	}

	public static void staticBiVoidCaller (int x) {
		GenBi<int,T>.staticVoidMethod (x);
	}

	public static float staticBiFloatCaller () {
		return GenBi<int,T>.staticFloatMethod ();
	}

	public static GenStruct<T> staticBiValueCaller (int x) {
		return GenBi<int,T>.staticValueMethod (x);
	}

	public static int staticSharedBiCaller (int x) {
		return GenBi<T,T>.staticMethod (x);
	}

	public static void staticSharedBiVoidCaller (int x) {
		GenBi<T,T>.staticVoidMethod (x);
	}

	public static float staticSharedBiFloatCaller () {
		return GenBi<T,T>.staticFloatMethod ();
	}

	public static GenStruct<T> staticSharedBiValueCaller (int x) {
		return GenBi<T,T>.staticValueMethod (x);
	}

	public static long staticBiLongCaller (long x) {
		return GenBi<int, T>.staticLongMethod (x);
	}

	public int structCaller (int x) {
		GenStruct<GenA<T>> gs = new GenStruct<GenA<T>> (123);

		return gs.method (x);
	}

	public T[] callInterface (IGen<T> ig) {
		return ig.iMethod ();
	}

	public void callVoidInterface (IGen<T> ig, int x) {
		ig.voidIMethod (x);
	}

	public long callLongInterface (IGen<T> ig, long x) {
		return ig.longIMethod (x);
	}

	public float callFloatInterface (IGen<T> ig) {
		return ig.floatIMethod ();
	}

	public GenStruct<T> callValueInterface (IGen<T> ig, int x) {
		return ig.valueIMethod (x);
	}
}

public class GenB<T> {
	public static int field = 123;
}

public class GenC<T> {
	public static int field ;

	static GenC () {
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
		return NonGen.field;
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
	delegate void ActionDelegate ();

	static bool haveError = false;

	static void error (string message) {
		haveError = true;
		Console.WriteLine (message);
	}

	static void typeCheck (String method, Object obj, Type t) {
		if (obj.GetType () != t)
			error ("object from " + method + " should have type " + t.ToString () + " but has type " + obj.GetType ().ToString ());
	}

	static int callStaticMethod<T> () {
		return GenA<T>.staticMethod ();
	}

	static void checkException<T> (String method, ActionDelegate action) where T : Exception {
		try {
			try {
				action ();
			} catch (T) {
				return;
			}
		} catch (Exception exc) {
			error ("method " + method + " should have thrown " + typeof (T).ToString () + " but did throw " + exc);
		}
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

		GenStruct<T>.staticField = 321;
		if (ga.getGenStructStaticField () != 321)
			error ("getGenStructStaticField");
		GenStruct<T>.staticField = -1;

		ga.hash (obj);

		if (!comp.Equals (ga.ident (obj), obj))
			error ("ident");

		if (!comp.Equals (ga.cast (obj), obj))
			error ("cast");
		if (typeof (T).IsValueType) {
			checkException<NullReferenceException> ("cast null value", delegate { ga.cast (null); });
		} else {
			if (ga.cast (null) != null)
				error ("cast null");
		}

		GenStruct<T> genstructt = new GenStruct<T> (453);
		if (ga.structCast ((object)genstructt).field != 453)
			error ("structCast");
		checkException<NullReferenceException> ("structCast null", delegate { ga.structCast (null); });

		if (ga.makeNullable ((object)genstructt).Value.field != 453)
			error ("makeNullable");
		if (ga.makeNullable (null) != null)
			error ("makeNullable null");

		if (ga.ldtokenT () != typeof (T))
			error ("ldtokenT");
		if (ga.ldtokenIGenT () != typeof (IGen<T>))
			error ("ldtokenIGenT");
		if (ga.ldtokenGenAIGenT () != typeof (GenA<IGen<T>>))
			error ("ldtokenGenAIGenT");
		if (ga.ldtokenGenB () != typeof (GenB<>))
			error ("ldtokenGenB");

		if (callStaticMethod<T> () != 54321)
			error ("staticMethod");

		GenBi<int,T>.field = 123;
		if (GenA<T>.staticBiCaller (123) != 246)
			error ("staticBiCaller");
		GenA<T>.staticBiVoidCaller (1234);
		if (GenBi<int,T>.field != 1234)
			error ("staticBiVoidCaller");
		if (GenA<T>.staticBiFloatCaller () != 1.0f)
			error ("staticBiFloatCaller");
		if (GenA<T>.staticBiLongCaller (123) != 123 + 1234)
			error ("staticBiLongCaller");
		GenStruct<T> gs = GenA<T>.staticBiValueCaller (987);
		if (gs.field != 987)
			error ("staticBiValueCaller");

		GenBi<T,T>.field = 123;
		if (GenA<T>.staticSharedBiCaller (123) != 246)
			error ("staticSharedBiCaller");
		GenA<T>.staticSharedBiVoidCaller (1234);
		if (GenBi<T,T>.field != 1234)
			error ("staticSharedBiVoidCaller");
		if (GenA<T>.staticSharedBiFloatCaller () != 1.0f)
			error ("staticSharedBiFloatCaller");
		GenStruct<T> gss = GenA<T>.staticSharedBiValueCaller (987);
		if (gss.field != 987)
			error ("staticSharedBiValueCaller");

		IntVoidDelegate ivdel = new IntVoidDelegate (GenA<T>.staticMethod);
		if (ivdel () != 54321)
			error ("staticMethod delegate");

		Type gatype = typeof (GenA<T>);
		MethodInfo staticMethodInfo = gatype.GetMethod ("staticMethod");
		if ((Convert.ToInt32 (staticMethodInfo.Invoke (null, null))) != 54321)
			error ("staticMethod reflection");

		if (GenA<T>.staticMethodCaller () != 54321)
			error ("staticMethodCaller");

		if (GenA<T>.staticFloatMethod () != 1.0)
			error ("staticFloatMethod");

		if (ga.structCaller (234) != 357)
			error ("structCaller");

		IGenImpl<T> igi = new IGenImpl<T> ();

		typeCheck ("callInterface", ga.callInterface (igi), typeof (T[]));
		if (ga.callLongInterface (igi, 345) != 346)
			error ("callLongInterface");
		GenStruct<T> gst = ga.callValueInterface (igi, 543);
		if (gst.field != 543)
			error ("callValueInterface");
		ga.callVoidInterface (igi, 654);
		if (igi.field != 654)
			error ("callVoidInterface");
		if (ga.callFloatInterface (igi) != 1.0f)
			error ("callFloatInterface");

		new GenADeriv<T> ();

		if (mustCatch) {
			checkException<GenExc<ClassA>> ("except", delegate { ga.except (); });
			checkException<GenExc<ClassA>> ("staticExcept", delegate { GenA<T>.staticExcept (); });
		} else {
			ga.except ();
			GenA<T>.staticExcept ();
		}

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
		work<ClassB> (new ClassB (), true);
		work<ClassC> (new ClassC (), true);
		work<GenA<ClassA>> (new GenA<ClassA> (), true);
		work<int[]> (new int[3], true);
		work<int> (123, true);
		work<int?> (123, true);
		work<GenStruct<ClassA>?> (new GenStruct<ClassA> (123), true);
		work<GenStruct<ClassA>?> (null, true);

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

}
