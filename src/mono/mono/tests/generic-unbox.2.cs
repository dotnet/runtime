using UnboxTest;

public class ClassA {};

public class main {
	public static int Main () {
		UnboxerStruct<ClassA> us;
		Unboxer<ClassA> u = new Unboxer<ClassA> ();

		us.field = 123;

		if (u.unbox ((object)us).field != 123)
			return 1;
		if (u.unboxNullable ((object)us).Value.field != 123)
			return 1;
		if (u.unboxNullable (null) != null)
			return 1;
		return 0;
	}
}
