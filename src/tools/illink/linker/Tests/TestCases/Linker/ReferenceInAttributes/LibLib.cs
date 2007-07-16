using System;

namespace LibLib {

	public class LibLibAttribute : Attribute {

		public Type LibLibType {
			[NotLinked] get { return null; }
			set {}
		}
	}

	public class BilBil {

		[NotLinked] public BilBil ()
		{
		}
	}
}

[NotLinked, AttributeUsage (AttributeTargets.All)]
class NotLinkedAttribute : Attribute {
}
