using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public class TypeOne
{
	static int z;
	public void GenericMethod<T> () {
		int foo = 10;
		z = foo;
	}

	public void SimpleMethod () { //thin EH table 
		int foo = 10;
		z = foo;
		try {
			z = 10;
		} catch (Exception e) {}
	}
}

public interface IFace
{
	void MyMethod ();
}


public abstract class TypeTwo
{
	public TypeTwo () { //2 simple EH entries
		try {
			new TypeOne ();
		} catch (Exception) {}

		try {
			new TypeOne ();
		} finally {}
		
	}
	[DllImport ("bla.dll")]
	public static extern void PInvoke ();
}

public abstract class AbsClass
{
	public AbsClass () { //fat EH table
		int z = 99;
		int foo = 10;
		z = foo;
		try {
			//make this bigger than 256 bytes, each entry is 25 bytes
			z = typeof(int).GetHashCode ();
			z = typeof(int).GetHashCode ();
			z = typeof(int).GetHashCode ();
			z = typeof(int).GetHashCode ();
			z = typeof(int).GetHashCode ();
			z = typeof(int).GetHashCode ();
			z = typeof(int).GetHashCode ();
			z = typeof(int).GetHashCode ();
			z = typeof(int).GetHashCode ();
			z = typeof(int).GetHashCode ();
			z = typeof(int).GetHashCode ();
			z = typeof(int).GetHashCode ();
		} finally {}
	}
	public abstract void AbsBla ();
}

public static class InternalCall
{
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern int ICall (object o);
}


public class ClassWithCCtor
{
	static ClassWithCCtor () {
	
	}
}

public class MethodWithLostsOfParams
{
	static void Foo(int a, int b, int c) {}
	static void Foo2(int a, int b, int c) {}
	static void Foo3(int a, int b, int c) {}
}

public class LastClass
{
	public static void Main ()
	{
	}
}