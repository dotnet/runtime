using System;

public class Foo : IFoo {

	public void Gazonk ()
	{
	}
}

public interface IFoo : IBar {
}

public interface IBar {

	void Gazonk ();
}

[NotLinked] public class Baz : IBaz {
}

[NotLinked] public interface IBaz {
}

[NotLinked, AttributeUsage (AttributeTargets.All)]
public class NotLinkedAttribute : Attribute {
}
