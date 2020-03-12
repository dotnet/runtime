using System.Collections.Generic;
using UnboxTest;
using BoxTest;

public class ClassA {}

public struct GenStruct<T> {
	T t;
	int field;

	public GenStruct (T _t, int _field) {
		t = _t;
		field = _field;
	}

	public bool isEqualTo (GenStruct<T> gs) {
		EqualityComparer<T> comp = EqualityComparer<T>.Default;

		return comp.Equals(gs.t, t) && gs.field == field;
	}
}

public class GenClass<T> {
	public object boxStruct (GenStruct<T> gs) {
		return (object)gs;
	}

	/*
	public object boxNullableStruct (GenStruct<T>? gs) {
		return (object)gs;
	}
	*/
}

public class main {
	public static int Main () {
		GenClass<int> gci = new GenClass<int> ();
		GenStruct<int> gsi = new GenStruct<int> (123, 456);

		if (!gsi.isEqualTo ((GenStruct<int>)gci.boxStruct (gsi)))
			return 1;
		/*
		if (!gsi.isEqualTo ((GenStruct<int>)gci.boxNullableStruct (gsi)))
			return 1;
		if (gci.boxNullableStruct (null) != null)
			return 1;
		*/

		GenClass<ClassA> gca = new GenClass<ClassA> ();
		GenStruct<ClassA> gsa = new GenStruct<ClassA> (new ClassA (), 789);

		if (!gsa.isEqualTo ((GenStruct<ClassA>)gca.boxStruct (gsa)))
			return 1;
		/*
		if (!gsa.isEqualTo ((GenStruct<ClassA>)gca.boxNullableStruct (gsa)))
			return 1;
		if (gca.boxNullableStruct (null) != null)
			return 1;
		*/

		UnboxerStruct<ClassA> us;
		Boxer<ClassA> b = new Boxer<ClassA> ();

		us.field = 123;

		if (((UnboxerStruct<ClassA>?)b.boxNullable (us)).Value.field != 123)
			return 1;
		if (b.boxNullable (null) != null)
			return 1;

		return 0;
	}
}
