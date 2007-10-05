using System.Collections.Generic;
using System;

class C<X,Y>
{
	class Q<A,B>
	{
		public  Type[] apply (C<X,Y> t)
		{
			return t.bar<A,B>();
		}
	}

	public  Type[] foo<A,B> ()
	{
		Q<A,B> q = new Q<A,B>();
		return q.apply(this);
	}

	public Type[] bar<A,B> ()
	{
		return new Type[] { typeof(X), typeof(Y), typeof(A), typeof(B) };
	}
}


class TypeofTest {
	public bool Bla() {
		Type t = typeof (Dictionary<,>);
		Type t2 = typeof (C<,>);
		return t.IsGenericTypeDefinition && t2.IsGenericTypeDefinition;
	}	
}

class TypeofTest2<X,Y> {
	public bool Bla() {
		Type t = typeof (Dictionary<,>);
		Type t2= typeof (C<,>);
		return t.IsGenericTypeDefinition && t2.IsGenericTypeDefinition;
	}	
}

class Driver {
	public static int Main () {
		C<int,string> c = new C<int,string>();
		Type[] types = c.foo<float,string> ();
		foreach (Type t in types)
			Console.WriteLine (t);
		if (types [0] != typeof (int))
			return 1;
		if (types [1] != typeof (string))
			return 2;
		if (types [2] != typeof (float))
			return 3;
		if (types [3] != typeof (string))
			return 4;

		if (!new TypeofTest().Bla())
			return 5;
		if (!new TypeofTest2<int, double>().Bla())
			return 6;

		return 0;
	}
}
