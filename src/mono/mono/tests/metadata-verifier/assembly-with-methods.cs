using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public class TypeOne
{
	public void GenericMethod<T> () {
	}

	public void SimpleMethod () {
	}
}

public interface IFace
{
	void MyMethod ();
}


public abstract class TypeTwo
{
	[DllImport ("bla.dll")]
	public static extern void PInvoke ();
}

public abstract class AbsClass
{
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