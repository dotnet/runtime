using System;
using System.Collections.Generic;

public class ClassA {}
public class ClassB {}
public class ClassC {}

public class NonGen {
	public static int field = 123;
}

public class GenA<T> {
	static T[] arr;

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
}

public class GenB<T> {
	public static int field = 345;
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

	public static void work<T> (T obj) {
		EqualityComparer<T> comp = EqualityComparer<T>.Default;

		GenA<T> ga = new GenA<T> ();

		typeCheck ("newGen", ga.newGen (), typeof (GenA<T>));
		typeCheck ("newGenInt", ga.newGenInt (), typeof (GenA<int>));
		typeCheck ("getArr", ga.getArr (), typeof (T[]));
		typeCheck ("newArr", ga.newArr (), typeof (T[]));
		//ga.newArrNested ();
		typeCheck ("newArrNested", ga.newArrNested (), typeof (GenB<GenB<T>>[]));

		if (ga.getGenField () != 345)
			error ("getGenField");

		if (ga.getNonGenField () != 123)
			error ("getNonGenField");

		ga.hash (obj);

		if (!comp.Equals (ga.ident (obj), obj))
			error ("ident");

		if (!comp.Equals (ga.cast (obj), obj))
			error ("cast");
	}

	public static int Main ()
	{
		work<ClassA> (new ClassA ());
		work<ClassB> (new ClassB ());
		work<ClassC> (new ClassC ());
		work<GenA<ClassA>> (new GenA<ClassA> ());
		work<int[]> (new int[3]);
		work<int> (123);

		if (haveError)
			return 1;
		return 0;
	}
}
