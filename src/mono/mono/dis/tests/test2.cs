/* Bug #76671
   Note: gmcs currently emits duplicate TypeSpecs, so this
	 case doesn't get exposed, so use csc compiled
	 assemblies till gmcs is fixed.

   Array of type params
*/
using System;

class list <T> {
	public static void bar ()
	{
		gen<int, T[][]>.foo ();
		gen<int[][], T>.foo ();
		gen<int, T[][,]>.foo ();
		gen<T[,,], int>.foo ();
	}
}

class list_two <D> {
	public static void bar ()
	{
		gen<int, D[][]>.foo ();
		gen<int[][], D>.foo ();
		gen<int, D[][,]>.foo ();
		gen<D[,,], int>.foo ();
	}
}

class list_three <F> {
	public static void bar ()
	{
		gen<int, F[][]>.foo ();
		gen<int[][], F>.foo ();
		gen<int, F[][,]>.foo ();
		gen<F[,,], int>.foo ();
	}
}

class gen <Q, R> {
	public static void foo () 
	{
	}
}

class Test {
	public static void Main ()
	{
	}
}
