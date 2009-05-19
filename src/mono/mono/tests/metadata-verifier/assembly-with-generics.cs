using System;
using System.Runtime.InteropServices;

public class Simple<T,K>  {
	public static void Gen<D>() {
		Simple<D,T>.Gen<K> ();
	}
	public T t;
}

public class Generic<A,B,C,D,E> 
	where B : class
	where C : struct
	where D : new()
	where E : class, new()
{
}


public class Driver
{
	public void GenericMethod<A,B,C,D,E> ()
		where B : class
		where C : struct
		where D : new()
		where E : class, new() 
	{
	}

	public static void Main ()
	{
		var x = new Simple<int, double> ();
		var y = x.t;
		Simple<int, double>.Gen<string> ();
		Simple<int, object>.Gen<Type> ();
	}
}