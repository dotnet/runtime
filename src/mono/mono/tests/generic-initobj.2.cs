using System.Collections.Generic;

public class ClassA {};
public class ClassB {};

public struct GenStruct<T> {
	public int field1;
	public T field2;
	public byte field3;
	public long field4;
}

public class Gen<T> {
	public static T getDefault () {
		return default (T);
	}

	public static GenStruct<T> getDefaultStruct () {
		return default (GenStruct<T>);
	}
}

public class main {
	public static bool isDefaultStruct<T> (GenStruct<T> gs) {
		EqualityComparer<T> eq = EqualityComparer<T>.Default;

		return gs.field1 == 0 && eq.Equals (gs.field2, default (T)) && gs.field3 == 0 && gs.field4 == 0;
	}

	public static int Main () {
		if (Gen<ClassA>.getDefault () != null)
			return 1;
		if (Gen<ClassB>.getDefault () != null)
			return 1;
		if (Gen<int>.getDefault () != 0)
			return 1;
		if (!isDefaultStruct<ClassA> (Gen<ClassA>.getDefaultStruct ()))
			return 1;
		if (!isDefaultStruct<ClassB> (Gen<ClassB>.getDefaultStruct ()))
			return 1;
		if (!isDefaultStruct<int> (Gen<int>.getDefaultStruct ()))
			return 1;
		return 0;
	}
}
