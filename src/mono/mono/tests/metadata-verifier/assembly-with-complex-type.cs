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
	const string contant_field = "333";
	[MarshalAs (UnmanagedType.Struct)] int bla;

	int[] z = new int[] {1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16};
}