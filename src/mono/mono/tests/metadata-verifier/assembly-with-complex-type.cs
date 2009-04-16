using System;

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

public class LastType
{
	int more_fields;
	int bla;
}