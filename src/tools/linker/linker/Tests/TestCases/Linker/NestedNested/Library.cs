using System;

public class Foo {
}

[NotLinked] public class Bar {

	[NotLinked] public class Baz {

		[NotLinked] public class Gazonk {
		}
	}
}

[NotLinked, AttributeUsage (AttributeTargets.All)]
public class NotLinkedAttribute : Attribute {
}
