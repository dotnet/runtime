using System;
using LibLib;

public class BarAttribute : Attribute {

	public BarAttribute ()
	{
	}

	public BarAttribute (Type type)
	{
	}

	public Type FieldType;

	public Type PropertyType {
		[NotLinked] get { return null; }
		set {}
	}
}

[Bar (typeof (Guy_A))]
public class Foo {

	[Bar (FieldType = typeof (Guy_B))]
	public Foo a;

	[Bar (PropertyType = typeof (Guy_C))]
	public Foo b;

	[LibLib (LibLibType = typeof (BilBil))]
	public Foo c;

	[LibLib (LibLibType = typeof (Guy_D))]
	public Foo d;
}

public class Guy_A {

	[NotLinked] public Guy_A ()
	{
	}
}

public class Guy_B {

	[NotLinked] public Guy_B ()
	{
	}
}

public class Guy_C {

	[NotLinked] public Guy_C ()
	{
	}
}

public class Guy_D {

	[NotLinked] public Guy_D ()
	{
	}
}

[NotLinked] public class Baz {
}

[NotLinked, AttributeUsage (AttributeTargets.All)]
class NotLinkedAttribute : Attribute {
}
