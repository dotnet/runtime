using System;

namespace Foo {

	[NotLinked] public class BarBar {
	}

	public class FooBaz {

		public FooBaz ()
		{
		}

		[NotLinked] public void BarBaz ()
		{
		}
	}

	public class TrucBaz {

		public TrucBaz ()
		{
		}
	}

	public class BazBaz {

		public BazBaz ()
		{
		}
	}
}

[NotLinked, AttributeUsage (AttributeTargets.All)]
public class NotLinkedAttribute : Attribute {
}
