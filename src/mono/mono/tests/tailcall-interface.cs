/*
Author:
    Jay Krell (jaykrell@microsoft.com)

Copyright 2018 Microsoft
Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
using System;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.MethodImplOptions;

public struct Point
{
	public int x;
	public int y;
}

interface I1
{
	void perturb_interface_offset1 ( );

	[MethodImpl (NoInlining)]
	int F1 (I2 i2, long counter, long initial_stack, long current_stack = 0);

	[MethodImpl (NoInlining)]
	int GF1<TF> (I2 i2, long counter, long initial_stack, long current_stack = 0);
}

interface GI1<TC>
{
	void perturb_interface_offset1 ( );
	void perturb_interface_offset2 ( );

	[MethodImpl (NoInlining)]
	int F1 (GI2<TC> i2, long counter, long initial_stack, long current_stack = 0);

	[MethodImpl (NoInlining)]
	int GF1<TF> (GI2<TC> i2, long counter, long initial_stack, long current_stack = 0);

	[MethodImpl (NoInlining)]
	int HF1<TF> (GI2<TF> i2, long counter, long initial_stack, long current_stack = 0);
}

interface I2
{
	void perturb_interface_offset1 ( );
	void perturb_interface_offset2 ( );
	void perturb_interface_offset3 ( );

	[MethodImpl (NoInlining)]
	int F2 (I1 i1, long counter, long initial_stack, long current_stack = 0);

	[MethodImpl (NoInlining)]
	int GF2<TF> (I1 i1, long counter, long initial_stack, long current_stack = 0);
}

interface GI2<TC>
{
	void perturb_interface_offset1 ( );
	void perturb_interface_offset2 ( );
	void perturb_interface_offset3 ( );
	void perturb_interface_offset4 ( );

	[MethodImpl (NoInlining)]
	int F2 (GI1<TC> i1, long counter, long initial_stack, long current_stack = 0);

	[MethodImpl (NoInlining)]
	int GF2<TF> (GI1<TC> i1, long counter, long initial_stack, long current_stack = 0);

	[MethodImpl (NoInlining)]
	int HF2<TF> (GI1<TF> i1, long counter, long initial_stack, long current_stack = 0);
}

unsafe public class C1 : I1
{
	void I1.perturb_interface_offset1 ( ) { }

	static int i;
	static public int errors;

	public static int check (long stack1, long stack2)
	{
// NOTE: This is odd in order to feed into the edited IL.
		++i;
		if (stack1 != 0)
			return 0;
		int error = (stack1 != stack2) ? 1 : 0;
		if (error == 0)
			return 0;
		errors += 1;
		Console.WriteLine ("{0} tailcall failure", i);
		return error;
	}

	[MethodImpl (NoInlining)]
	int I1.F1 (I2 i2, long counter, long initial_stack, long current_stack)
	{
		if (counter > 0)
			return i2.F2 (this, counter - 1, initial_stack, (long)&counter);
		return check ((long)&counter, current_stack);
	}

	[MethodImpl (NoInlining)]
	int I1.GF1<TF> (I2 i2, long counter, long initial_stack, long current_stack)
	{
		if (counter > 0)
			return i2.GF2<TF> (this, counter - 1, initial_stack, (long)&counter);
		return check ((long)&counter, current_stack);
	}
}

unsafe public class GC1<TC> : GI1<TC>
{
	void GI1<TC>.perturb_interface_offset1 ( ) { }
	void GI1<TC>.perturb_interface_offset2 ( ) { }

	public static int check (long stack1, long stack2)
	{
		return C1.check (stack1, stack2);
	}

	[MethodImpl (NoInlining)]
	int GI1<TC>.F1 (GI2<TC> i2, long counter, long initial_stack, long current_stack)
	{
		if (counter > 0)
			return i2.F2 (this, counter - 1, initial_stack, (long)&counter);
		return check ((long)&counter, current_stack);
	}

	[MethodImpl (NoInlining)]
	int GI1<TC>.GF1<TF> (GI2<TC> i2, long counter, long initial_stack, long current_stack)
	{
		if (counter > 0)
			return i2.GF2<TF> (this, counter - 1, initial_stack, (long)&counter);
		return check ((long)&counter, current_stack);
	}

	[MethodImpl (NoInlining)]
	int GI1<TC>.HF1<TF> (GI2<TF> i2, long counter, long initial_stack, long current_stack)
	{
		if (counter > 0)
			return i2.HF2<TC> (this, counter - 1, initial_stack, (long)&counter);
		return check ((long)&counter, current_stack);
	}
}

unsafe public class C2 : I2
{
	void I2.perturb_interface_offset1 ( ) { }
	void I2.perturb_interface_offset2 ( ) { }
	void I2.perturb_interface_offset3 ( ) { }

	static int check (long stack1, long stack2)
	{
		return C1.check (stack1, stack2);
	}

	[MethodImpl (NoInlining)]
	int I2.F2 (I1 i1, long counter, long initial_stack, long current_stack)
	{
		if (counter > 0)
			return i1.F1 (this, counter - 1, initial_stack, (long)&counter);
		return check ((long)&counter, current_stack);
	}

	[MethodImpl (NoInlining)]
	int I2.GF2<TF> (I1 i1, long counter, long initial_stack, long current_stack)
	{
		if (counter > 0)
			return i1.GF1<TF> (this, counter - 1, initial_stack, (long)&counter);
		return check ((long)&counter, current_stack);
	}
}

unsafe public class GC2<TC> : GI2<TC>
{
	void GI2<TC>.perturb_interface_offset1 ( ) { }
	void GI2<TC>.perturb_interface_offset2 ( ) { }
	void GI2<TC>.perturb_interface_offset3 ( ) { }
	void GI2<TC>.perturb_interface_offset4 ( ) { }

	public static int check (long stack1, long stack2)
	{
		return C1.check (stack1, stack2);
	}

	[MethodImpl (NoInlining)]
	int GI2<TC>.F2 (GI1<TC> i1, long counter, long initial_stack, long current_stack)
	{
		if (counter > 0)
			return i1.F1 (this, counter - 1, initial_stack, (long)&counter);
		return check ((long)&counter, current_stack);
	}

	[MethodImpl (NoInlining)]
	int GI2<TC>.GF2<TF> (GI1<TC> i1, long counter, long initial_stack, long current_stack)
	{
		if (counter > 0)
			return i1.GF1<TF> (this, counter - 1, initial_stack, (long)&counter);
		return check ((long)&counter, current_stack);
	}

	[MethodImpl (NoInlining)]
	int GI2<TC>.HF2<TF> (GI1<TF> i1, long counter, long initial_stack, long current_stack)
	{
		if (counter > 0)
			return i1.HF1<TC> (this, counter - 1, initial_stack, (long)&counter);
		return check ((long)&counter, current_stack);
	}
}

public class A { }

public class B { }

interface IC
{
	[MethodImpl (NoInlining)]
	T cast1<T> (object o, long counter = 100, long stack = 0);

	[MethodImpl (NoInlining)]
	B cast2 (object o, long counter = 100, long stack = 0);

	[MethodImpl (NoInlining)]
	T cast3<T> (object o, long counter = 100, long stack = 0);

	[MethodImpl (NoInlining)]
	B[] cast4 (object o, long counter = 100, long stack = 0);

	[MethodImpl (NoInlining)]
	T[] cast5<T> (object o, long counter = 100, long stack = 0);
}

unsafe public class C
{
	[MethodImpl (NoInlining)]
	public static void check (long stack1, long stack2)
	{
		C1.check (stack1, stack2);
	}

	[MethodImpl (NoInlining)]
	public T cast1<T> (object o, long counter = 100, long stack = 0)
	{
		if (counter > 0)
			return cast1<T> (o, counter - 1, (long)&counter);
		check ((long)&counter, stack);
		return (T)o;
	}

	[MethodImpl (NoInlining)]
	public B cast2 (object o, long counter = 100, long stack = 0)
	{
		if (counter > 0)
			return cast2 (o, counter - 1, (long)&counter);
		check ((long)&counter, stack);
		return cast1<B> (o);
	}

	[MethodImpl (NoInlining)]
	public T cast3<T> (object o, long counter = 100, long stack = 0)
	{
		if (counter > 0)
			return cast3<T> (o, counter - 1, (long)&counter);
		check ((long)&counter, stack);
		return cast1<T> (o);
	}

	[MethodImpl (NoInlining)]
	public B[] cast4 (object o, long counter = 100, long stack = 0)
	{
		if (counter > 0)
			return cast4 (o, counter - 1, (long)&counter);
		check ((long)&counter, stack);
		return cast1<B[]> (o);
	}

	[MethodImpl (NoInlining)]
	public T[] cast5<T> (object o, long counter = 100, long stack = 0)
	{
		if (counter > 0)
			return cast5<T> (o, counter - 1, (long)&counter);
		check ((long)&counter, stack);
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
	public static T cast1<T> (object o, long counter = 100, long stack = 0)
	{
		if (counter > 0)
			return cast1<T> (o, counter - 1, (long)&counter);
		check ((long)&counter, stack);
		return (T)o;
	}

	[MethodImpl (NoInlining)]
	public B cast2 (object o, long counter = 100, long stack = 0)
	{
		if (counter > 0)
			return cast2 (o, counter - 1, (long)&counter);
		check ((long)&counter, stack);
		return cast1<B> (o);
	}

	[MethodImpl (NoInlining)]
	public T cast3<T> (object o, long counter = 100, long stack = 0)
	{
		if (counter > 0)
			return cast3<T> (o, counter - 1, (long)&counter);
		check ((long)&counter, stack);
		return cast1<T> (o);
	}

	[MethodImpl (NoInlining)]
	public B[] cast4 (object o, long counter = 100, long stack = 0)
	{
		if (counter > 0)
			return cast4 (o, counter - 1, (long)&counter);
		check ((long)&counter, stack);
		return cast1<B[]> (o);
	}

	[MethodImpl (NoInlining)]
	public T[] cast5<T> (object o, long counter = 100, long stack = 0)
	{
		if (counter > 0)
			return cast5<T> (o, counter - 1, (long)&counter);
		check ((long)&counter, stack);
		return cast1<T[]> (o);
	}

	[MethodImpl (NoInlining)]
	public T1 cast6 (object o, long counter = 100, long stack = 0)
	{
		if (counter > 0)
			return cast6 (o, counter - 1, (long)&counter);
		check ((long)&counter, stack);
		return cast1<T1> (o);
	}

	[MethodImpl (NoInlining)]
	public T1 cast7<T> (object o, long counter = 100, long stack = 0)
	{
		if (counter > 0)
			return cast7<T> (o, counter - 1, (long)&counter);
		check ((long)&counter, stack);
		return cast1<T1> (o);
	}

	[MethodImpl (NoInlining)]
	public T1[] cast8 (object o, long counter = 100, long stack = 0)
	{
		if (counter > 0)
			return cast8 (o, counter - 1, (long)&counter);
		check ((long)&counter, stack);
		return cast3<T1[]> (o);
	}

	[MethodImpl (NoInlining)]
	public T1[] cast9<T> (object o, long counter = 100, long stack = 0)
	{
		if (counter > 0)
			return cast9<T> (o, counter - 1, (long)&counter);
		check ((long)&counter, stack);
		return cast3<T1[]> (o);
	}
}

unsafe class C3
{
	int i;

	[MethodImpl (NoInlining)]
	void print (object o)
	{
		++i;
		//Console.WriteLine("{0} {1}", i, o);
		//Console.WriteLine(i);
	}

	[MethodImpl (NoInlining)]
	public void Main()
	{
		var da = new D<A> ();
		var db = new D<B> ();
		var dba = new D<B[]> ();
		var c = new C ();
		var b = new B ();
		var ba = new B [1];
		int stack;
		var c1 = (I1)new C1 ();
		var c2 = (I2)new C2 ();
		int result;
		var c1o = (GI1<object>)new GC1<object> ();
		var c1oa = (GI1<object[]>)new GC1<object[]> ();
		var c1i = (GI1<int>)new GC1<int> ();
		var c1ia = (GI1<int[]>)new GC1<int[]> ();
		var c1s = (GI1<Point>)new GC1<Point> ();
		var c1sa = (GI1<Point[]>)new GC1<Point[]> ();
		var c2o = (GI2<object>)new GC2<object> ();
		var c2oa = (GI2<object[]>)new GC2<object[]> ();
		var c2i = (GI2<int>)new GC2<int> ();
		var c2ia = (GI2<int[]>)new GC2<int[]> ();
		var c2s = (GI2<Point>)new GC2<Point> ();
		var c2sa = (GI2<Point[]>)new GC2<Point[]> ();

		print (da.cast2 (b));
		print (da.cast3<B> (b));
		print (da.cast3<B[]> (ba));
		print (da.cast4 (ba));
		print (da.cast5<B> (ba));

		print (db.cast6 (b));
		print (db.cast7<A> (b));
		print (dba.cast7<A[]> (ba));
		print (db.cast8 (ba));
		print (db.cast9<A> (ba));

		print (c.cast2 (b));
		print (c.cast3<B> (b));
		print (c.cast3<B[]> (ba));
		print (c.cast4 (ba));
		print (c.cast5<B> (ba));

		//Console.WriteLine("done");
		//Console.WriteLine("success");

		result = c1.F1 (c2, 100, (long)&stack, 0)
			+ c1.GF1<object> (c2, 100, (long)&stack, 0)
			+ c1.GF1<A> (c2, 100, (long)&stack, 0)
			+ c1.GF1<A[]> (c2, 100, (long)&stack, 0)
			+ c1.GF1<int> (c2, 100, (long)&stack, 0)
			+ c1.GF1<int[]> (c2, 100, (long)&stack, 0)

			+ c1o.F1 (c2o, 100, (long)&stack)
			+ c1o.GF1<object> (c2o, 100, (long)&stack)
			+ c1o.GF1<A> (c2o, 100, (long)&stack)
			+ c1o.GF1<A[]> (c2o, 100, (long)&stack)
			+ c1o.GF1<int> (c2o, 100, (long)&stack)
			+ c1o.GF1<int[]> (c2o, 100, (long)&stack)

			+ c1oa.GF1<object[]> (c2oa, 100, (long)&stack)
			+ c1oa.GF1<A> (c2oa, 100, (long)&stack)
			+ c1oa.GF1<A[]> (c2oa, 100, (long)&stack)
			+ c1oa.GF1<int> (c2oa, 100, (long)&stack)
			+ c1oa.GF1<int[]> (c2oa, 100, (long)&stack)

			+ c1i.GF1<object> (c2i, 100, (long)&stack)
			+ c1i.GF1<A> (c2i, 100, (long)&stack)
			+ c1i.GF1<A[]> (c2i, 100, (long)&stack)
			+ c1i.GF1<int> (c2i, 100, (long)&stack)
			+ c1i.GF1<int[]> (c2i, 100, (long)&stack)

			+ c1ia.GF1<object> (c2ia, 100, (long)&stack)
			+ c1ia.GF1<A> (c2ia, 100, (long)&stack)
			+ c1ia.GF1<A[]> (c2ia, 100, (long)&stack)
			+ c1ia.GF1<int> (c2ia, 100, (long)&stack)
			+ c1ia.GF1<int[]> (c2ia, 100, (long)&stack)

			+ c1s.GF1<object> (c2s, 100, (long)&stack)
			+ c1s.GF1<A> (c2s, 100, (long)&stack)
			+ c1s.GF1<A[]> (c2s, 100, (long)&stack)
			+ c1s.GF1<int> (c2s, 100, (long)&stack)
			+ c1s.GF1<int[]> (c2s, 100, (long)&stack)

			+ c1sa.GF1<object> (c2sa, 100, (long)&stack)
			+ c1sa.GF1<A> (c2sa, 100, (long)&stack)
			+ c1sa.GF1<A[]> (c2sa, 100, (long)&stack)
			+ c1sa.GF1<int> (c2sa, 100, (long)&stack)
			+ c1sa.GF1<int[]> (c2sa, 100, (long)&stack)
			+ C1.errors;

		//Console.WriteLine (result == 0 ? "success" : "failure {0}", result);

		Environment.Exit (result);
	}

	[MethodImpl (NoInlining)]
	public static void Main(string[] args)
	{
		new C3 ().Main ();
	}

}
