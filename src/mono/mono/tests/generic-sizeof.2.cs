public class ClassA {}
public struct GenStruct<T> {
	int field1;
	long field2;
	T field3;
}
public struct Struct {
	int field1;
	long field2;
	byte field3;
}

public class Gen<T> {
	public unsafe int sizeofGenStructT () { return sizeof (Struct); }
}

public class main {
	public static unsafe int Main () {
		Gen<ClassA> ga = new Gen<ClassA> ();

		if (ga.sizeofGenStructT () != sizeof (Struct))
			return 1;

		return 0;
	}
}
