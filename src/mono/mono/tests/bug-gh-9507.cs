using type_with_special_array_cast = System.UInt64; // MonoClass<ulong[]>::cast_class = long
using other_type = System.Double;

public class TestIsInst<T>
{
	public T[] array;

	public TestIsInst() {
		array = new T[16];

		if (array is other_type[]) // should not crash or throw NullReferenceException
			throw new System.Exception("Unreachable");
	}
}

public class Bug9507  // https://github.com/mono/mono/issues/9507
{
	public static void Main () {
		var table = new TestIsInst<type_with_special_array_cast> ();
	}
}
