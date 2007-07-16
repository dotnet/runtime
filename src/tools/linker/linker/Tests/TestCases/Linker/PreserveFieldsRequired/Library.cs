using System;

public class Foo {

	public Foo ()
	{
		new NotRequiredButUsedNotPreserved ();
		new NotRequiredButUsedAndFieldsPreserved ();
	}
}

public class NotRequiredButUsedNotPreserved {

	[NotLinked] public int foo;
	[NotLinked] public int bar;
}

public class NotRequiredButUsedAndFieldsPreserved {

	public int foo;
	public int bar;

	[NotLinked] public int FooBar ()
	{
		return foo + bar;
	}
}

[NotLinked] public class NotRequiredAndNotUsed {
}

[NotLinked, AttributeUsage (AttributeTargets.All)]
public class NotLinkedAttribute : Attribute {
}
