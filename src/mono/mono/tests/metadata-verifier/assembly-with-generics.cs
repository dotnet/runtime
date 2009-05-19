using System;
using System.Runtime.InteropServices;

public class Simple<T,K>  {}

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
	}
}