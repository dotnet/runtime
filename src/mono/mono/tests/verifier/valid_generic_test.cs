using System;

public delegate void GenericDelegate<T>(T a);
public delegate void ObjectDelegate(object a);

public class Tst<T> {
	T field;
	static T staticField;

	public Tst() {
		TestLoadStore (field);
		TestByRef (field);
		TestCalls ();
		TestTypeOps ();
		TestDelegate ();
	}

	public void TestLoadStore (T arg) {
		T local = arg;
		T[] array = new T[10];

		field = arg;
		staticField = arg;
		arg = local;
		arg = field;
		arg = staticField;
		array[0] = arg;
		field = array[1];
	}
	
	public void TestByRef (T arg) {
		T local = arg;
		T[] array = new T[10];
		PassByRef (ref arg);
		PassByRef (ref array[0]);
		PassByRef (ref field);
		PassByRef (ref local);
		PassByRef (ref staticField);
	}

	public void TestCalls () {
		this.field.ToString ();
		this.field = Static ();
		
		Virtual (field);
	}

	public void TestTypeOps () {
		object o = typeof (T);
		o = field;
		o = default(T);
		staticField = (T)o;
		if (o is T) {
			//T x = o as T;	test with constraints
		}
	}

	public void TestDelegate () {
		GenericDelegate<T> gd = new GenericDelegate<T>(this.Virtual);
		gd(field);
	}

	public void PassByRef (ref T t) {
		t = default (T);
	}

	public virtual void Virtual (T a) {
	}

	public static T Static() {
		return staticField;
	}
}

public struct Foo {
	public int dd;
}

public class Driver {
	public static void Main () {
		new Tst<int> ().Virtual (10);
		new Tst<string> ().Virtual ("str");
		new Tst<Foo> ().Virtual (new Foo ());
		
	}
}
