using System;

public class C { }

public struct S { }

public class GenType<T> : IComparable<GenType<T>>
#if ADD_STRUCT_CONSTRAINT
    where T : struct
#endif
{
	public int CompareTo(GenType<T> to)
	{
		return -1;
	}

	public void foo()
	{
		Console.WriteLine(typeof(GenType<T>).ToString() + ".foo");
	}
}

public class cs1
{
	public int m_i;

	public static int Main(String [] args)
	{
#if ADD_STRUCT_CONSTRAINT
		GenType<S> g = new GenType<S>();
		Console.WriteLine(Type.GetType("System.IComparable`1[GenType`1[S]]"));
#else
		GenType<C> g = new GenType<C>();
		Console.WriteLine(Type.GetType("System.IComparable`1[GenType`1[C]]"));
#endif
		g.foo();

		Console.WriteLine("PASS");

		return 100;
	}
}