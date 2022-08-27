using System;
using System.Runtime.InteropServices;
public class TypeOne
{
	int instance_field;
	static int static_field;

	public static void Main ()
	{
	}


	public long SimpleMethod (int a, double b) {
		return a;
	}
}

public interface Iface
{
	void Foo ();
}


public class OtherType
{
	int more_fields;
	int bla;
}

public class TypeWithFunkyStuff
{
	const string constant_field = "333";
	[MarshalAs (UnmanagedType.Struct)] int bla;

	int[] z = new int[] {1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16};
}

public enum Foo
{
	A,B,C
}

public class Bla : Iface
{
	public void Foo () {
		int a,b,c;
		a = b = c = 0;
	}
}

[StructLayout (LayoutKind.Sequential, Pack=8)]
public class SequentialLayout {
	int a;
	int b;
}

[StructLayout (LayoutKind.Sequential, Pack=4, Size=20)]
public class SequentialLayout2 {
	int a;
	int b;
}
[StructLayout (LayoutKind.Explicit)]
public class ExplicitLayout2 {
	[FieldOffset (33)] int a;
	[FieldOffset (0)] int b;
}

public class ZZ : Iface
{
	void Iface.Foo () {
	}
}

public class Generic<T> {

}

public class NonGeneric {

	public static object Bla ()
	{
		Generic<int> f = new Generic<int> ();
		return f;
	}
}

public class OuterType {
	public class InnerType {}
}