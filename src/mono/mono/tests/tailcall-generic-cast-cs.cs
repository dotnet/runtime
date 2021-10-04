/*
Test tailcall-generic-cast-cs.cs.

Author:
    Jay Krell (jaykrell@microsoft.com)

Copyright 2018 Microsoft
Licensed under the MIT license. See LICENSE file in the project root for full license information.

Test many (but not all) variations of tail calls with generics and vtable_arg.
Yet missing here is actual virtual functions.

This is compiled both from C# w/o tail. prefixes and IL w/.
The C# code does not presently check for tail calling.
*/
using System;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.MethodImplOptions;

public class A { }

public class B { }

unsafe public class C
{
	static int i;

	[MethodImpl (NoInlining)]
	public static void check (long stack1, long stack2)
	{
// NOTE: This is mangled in order to feed into the edited IL.
		++i;
		if (stack1 != 0) 		// remove from IL
			return;			// remove from IL
		if (stack1 != stack2) {
			Console.WriteLine ("{0} tailcall failure", i);
			Environment.Exit (1);
			return;
		}
		//Console.WriteLine ("{0} tailcall success", ++i);
	}

	[MethodImpl (NoInlining)]
	public static T cast1<T> (object o, int counter = 100, long stack = 0)
	{
		int local;
		if (counter > 0)
			return cast1<T> (o, counter - 1, (long)&local);
		check ((long)&local, stack);
		return (T)o;
	}

	[MethodImpl (NoInlining)]
	public static B cast2 (object o, int counter = 100, long stack = 0)
	{
		int local;
		if (counter > 0)
			return cast2 (o, counter - 1, (long)&local);
		check ((long)&local, stack);
		return cast1<B> (o);
	}

	[MethodImpl (NoInlining)]
	public static T cast3<T> (object o, int counter = 100, long stack = 0)
	{
		int local;
		if (counter > 0)
			return cast3<T> (o, counter - 1, (long)&local);
		check ((long)&local, stack);
		return cast1<T> (o);
	}

	[MethodImpl (NoInlining)]
	public static B[] cast4 (object o, int counter = 100, long stack = 0)
	{
		int local;
		if (counter > 0)
			return cast4 (o, counter - 1, (long)&local);
		check ((long)&local, stack);
		return cast1<B[]> (o);
	}

	[MethodImpl (NoInlining)]
	public static T[] cast5<T> (object o, int counter = 100, long stack = 0)
	{
		int local;
		if (counter > 0)
			return cast5<T> (o, counter - 1, (long)&local);
		check ((long)&local, stack);
		return cast1<T[]> (o);
	}
}

unsafe public class D<T1>
{
	[MethodImpl (NoInlining)]
	public static void check (long stack1, long stack2)
	{
		C.check (stack1, stack2);
	}

	[MethodImpl (NoInlining)]
	public static T cast1<T> (object o, int counter = 100, long stack = 0)
	{
		int local;
		if (counter > 0)
			return cast1<T> (o, counter - 1, (long)&local);
		check ((long)&local, stack);
		return (T)o;
	}

	[MethodImpl (NoInlining)]
	public static B cast2 (object o, int counter = 100, long stack = 0)
	{
		int local;
		if (counter > 0)
			return cast2 (o, counter - 1, (long)&local);
		check ((long)&local, stack);
		return cast1<B> (o);
	}

	[MethodImpl (NoInlining)]
	public static T cast3<T> (object o, int counter = 100, long stack = 0)
	{
		int local;
		if (counter > 0)
			return cast3<T> (o, counter - 1, (long)&local);
		check ((long)&local, stack);
		return cast1<T> (o);
	}

	[MethodImpl (NoInlining)]
	public static B[] cast4 (object o, int counter = 100, long stack = 0)
	{
		int local;
		if (counter > 0)
			return cast4 (o, counter - 1, (long)&local);
		check ((long)&local, stack);
		return cast1<B[]> (o);
	}

	[MethodImpl (NoInlining)]
	public static T[] cast5<T> (object o, int counter = 100, long stack = 0)
	{
		int local;
		if (counter > 0)
			return cast5<T> (o, counter - 1, (long)&local);
		check ((long)&local, stack);
		return cast1<T[]> (o);
	}

	[MethodImpl (NoInlining)]
	public static T1 cast6 (object o, int counter = 100, long stack = 0)
	{
		int local;
		if (counter > 0)
			return cast6 (o, counter - 1, (long)&local);
		check ((long)&local, stack);
		return cast1<T1> (o);
	}

	[MethodImpl (NoInlining)]
	public static T1 cast7<T> (object o, int counter = 100, long stack = 0)
	{
		int local;
		if (counter > 0)
			return cast7<T> (o, counter - 1, (long)&local);
		check ((long)&local, stack);
		return cast1<T1> (o);
	}

	[MethodImpl (NoInlining)]
	public static T1[] cast8 (object o, int counter = 100, long stack = 0)
	{
		int local;
		if (counter > 0)
			return cast8 (o, counter - 1, (long)&local);
		check ((long)&local, stack);
		return cast3<T1[]> (o);
	}

	[MethodImpl (NoInlining)]
	public static T1[] cast9<T> (object o, int counter = 100, long stack = 0)
	{
		int local;
		if (counter > 0)
			return cast9<T> (o, counter - 1, (long)&local);
		check ((long)&local, stack);
		return cast3<T1[]> (o);
	}
}

public class E
{
	static int i;

	[MethodImpl (NoInlining)]
	static void print (object o)
	{
		++i;
		//Console.WriteLine("{0} {1}", i, o);
		//Console.WriteLine(i);
	}

	[MethodImpl (NoInlining)]
	public static void Main(string[] args)
	{
		print (C.cast2 (new B()));
		print (C.cast3<B> (new B()));
		print (C.cast3<B[]> (new B[1]));
		print (C.cast4 (new B[1]));
		print (C.cast5<B> (new B [1]));

		print (D<A>.cast2 (new B()));
		print (D<A>.cast3<B> (new B()));
		print (D<A>.cast3<B[]> (new B[1]));
		print (D<A>.cast4 (new B[1]));
		print (D<A>.cast5<B> (new B [1]));

		print (D<B>.cast6 (new B()));
		print (D<B>.cast7<A> (new B()));
		print (D<B[]>.cast7<A[]> (new B[1]));
		print (D<B>.cast8 (new B[1]));
		print (D<B>.cast9<A> (new B [1]));

		//Console.WriteLine("done");
		//Console.WriteLine("success");
	}
}
